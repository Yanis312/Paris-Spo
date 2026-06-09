using Microsoft.Extensions.Logging;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.Infrastructure.ExternalApis;

public class MatchSyncService
{
    private readonly IFootballDataService _footballData;
    private readonly SportApi7Service _sportApi7;
    private readonly IMatchRepository _matchRepo;
    private readonly ILogger<MatchSyncService> _logger;

    public MatchSyncService(
        IFootballDataService footballData,
        SportApi7Service sportApi7,
        IMatchRepository matchRepo,
        ILogger<MatchSyncService> logger)
    {
        _footballData = footballData;
        _sportApi7 = sportApi7;
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
        _logger.LogInformation("WC: {Total} fetched, {Known} with known teams", matches.Count, known.Count);

        foreach (var match in known)
        {
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

    public async Task<Match?> SyncMatchOddsAsync(string matchId)
    {
        var match = await _matchRepo.GetByIdAsync(matchId);
        if (match == null) return null;

        match.Odds = await _sportApi7.GetOddsByEventIdAsync(match.ApiFootballId);
        await _matchRepo.UpsertAsync(match);
        return match;
    }
}
