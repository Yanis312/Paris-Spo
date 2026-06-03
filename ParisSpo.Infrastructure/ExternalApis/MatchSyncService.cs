using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.Infrastructure.ExternalApis;

public class MatchSyncService
{
    private readonly IFootballDataService _footballData;
    private readonly IOddsService _oddsService;
    private readonly IMatchRepository _matchRepo;

    public MatchSyncService(
        IFootballDataService footballData,
        IOddsService oddsService,
        IMatchRepository matchRepo)
    {
        _footballData = footballData;
        _oddsService = oddsService;
        _matchRepo = matchRepo;
    }

    public async Task<List<Match>> SyncTodayMatchesAsync()
    {
        var matches = await _footballData.GetTodayMatchesAsync();

        foreach (var match in matches)
        {
            var odds = await _oddsService.GetOddsForMatchAsync(
                match.HomeTeamName, match.AwayTeamName, match.KickOff);
            match.Odds = odds;
            await _matchRepo.UpsertAsync(match);
        }

        return matches;
    }

    public async Task<Match?> SyncMatchOddsAsync(string matchId)
    {
        var match = await _matchRepo.GetByIdAsync(matchId);
        if (match == null) return null;

        var odds = await _oddsService.GetOddsForMatchAsync(
            match.HomeTeamName, match.AwayTeamName, match.KickOff);
        match.Odds = odds;
        await _matchRepo.UpsertAsync(match);
        return match;
    }
}
