using Microsoft.Extensions.Logging;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.Infrastructure.ExternalApis;

public class MatchSyncService
{
    private readonly IFootballDataService _footballData;
    private readonly SportApi7Service _sportApi7;
    private readonly TheOddsApiService _oddsApi;
    private readonly IMatchRepository _matchRepo;
    private readonly ILogger<MatchSyncService> _logger;

    public MatchSyncService(
        IFootballDataService footballData,
        SportApi7Service sportApi7,
        TheOddsApiService oddsApi,
        IMatchRepository matchRepo,
        ILogger<MatchSyncService> logger)
    {
        _footballData = footballData;
        _sportApi7 = sportApi7;
        _oddsApi = oddsApi;
        _matchRepo = matchRepo;
        _logger = logger;
    }

    public Task<List<Match>> SyncTodayMatchesAsync() => SyncMatchesByDateAsync(DateTime.UtcNow);

    /// <summary>Charge les 104 matchs de la Coupe du Monde 2026 dans MongoDB.</summary>
    public async Task<List<Match>> SyncWorldCupAsync()
    {
        if (_footballData is not FootballDataService fd)
        {
            _logger.LogWarning("WorldCup sync requires FootballDataService");
            return [];
        }

        List<Match> matches;
        try
        {
            matches = await fd.GetAllWorldCupMatchesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch World Cup matches");
            throw;
        }

        // garde seulement matchs avec équipes connues (exclut TBD phases finales)
        var known = matches.Where(m => m.HomeTeamName != "?" && m.AwayTeamName != "?").ToList();

        // Récupère TOUTES les cotes WC en 1 appel The Odds API
        var oddsMap = await _oddsApi.GetWorldCupOddsMapAsync();
        _logger.LogInformation("WC: {Total} fetched, {Known} known, {Odds} odds entries",
            matches.Count, known.Count, oddsMap.Count);

        foreach (var match in known)
        {
            // matche les cotes par nom d'équipe (normalisé)
            var odds = MatchOddsTo(match, oddsMap);
            if (odds != null) match.Odds = odds;

            try { await _matchRepo.UpsertAsync(match); }
            catch (Exception ex) { _logger.LogError(ex, "Failed WC match {Id}", match.ApiFootballId); }
        }
        return known;
    }

    public async Task<List<Match>> SyncMatchesByDateAsync(DateTime date)
    {
        List<Match> matches;
        try
        {
            matches = await _footballData.GetMatchesByDateAsync(date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch matches for {Date}", date);
            throw;
        }

        // Fetch odds only for scheduled matches, capped to protect RapidAPI quota
        var oddsEligible = matches
            .Where(m => m.Status == Domain.Enums.MatchStatus.Scheduled)
            .Take(10)
            .Select(m => m.ApiFootballId)
            .ToHashSet();

        _logger.LogInformation("Fetched {Count} matches for {Date}", matches.Count, date);

        foreach (var match in matches)
        {
            try
            {
                if (oddsEligible.Contains(match.ApiFootballId))
                    match.Odds = await _sportApi7.GetOddsByEventIdAsync(match.ApiFootballId);

                await _matchRepo.UpsertAsync(match);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed match {Home} vs {Away} (id={Id})",
                    match.HomeTeamName, match.AwayTeamName, match.ApiFootballId);
            }
        }

        return matches;
    }

    // matche les cotes (clé "home|away" normalisée) à un match, avec fuzzy sur les noms
    private static List<MatchOdds>? MatchOddsTo(Match m, Dictionary<string, List<MatchOdds>> oddsMap)
    {
        var h = Simplify(m.HomeTeamName);
        var a = Simplify(m.AwayTeamName);

        foreach (var kvp in oddsMap)
        {
            var parts = kvp.Key.Split('|');
            if (parts.Length != 2) continue;
            var oh = parts[0]; var oa = parts[1];
            // match si un nom contient l'autre (gère "USA" vs "United States", "Korea" vs "Korea Republic")
            bool homeMatch = oh.Contains(h) || h.Contains(oh);
            bool awayMatch = oa.Contains(a) || a.Contains(oa);
            if (homeMatch && awayMatch) return kvp.Value;
        }
        return null;
    }

    private static string Simplify(string s)
        => new(s.ToLowerInvariant().Where(char.IsLetter).ToArray());

    public async Task<Match?> SyncMatchOddsAsync(string matchId)
    {
        var match = await _matchRepo.GetByIdAsync(matchId);
        if (match == null) return null;

        match.Odds = await _sportApi7.GetOddsByEventIdAsync(match.ApiFootballId);
        await _matchRepo.UpsertAsync(match);
        return match;
    }
}
