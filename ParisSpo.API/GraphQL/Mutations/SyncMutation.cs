using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.ExternalApis;

namespace ParisSpo.API.GraphQL.Mutations;

[MutationType]
public class SyncMutation
{
    public async Task<List<Match>> SyncTodayMatchesAsync([Service] MatchSyncService syncService)
        => await syncService.SyncTodayMatchesAsync();

    public async Task<Match?> SyncMatchOddsAsync(string matchId, [Service] MatchSyncService syncService)
        => await syncService.SyncMatchOddsAsync(matchId);

    public async Task<List<Match>> SyncMatchesByDateAsync(DateTime date, [Service] MatchSyncService syncService)
        => await syncService.SyncMatchesByDateAsync(date);

    public async Task<List<Match>> SyncWorldCupAsync([Service] MatchSyncService syncService)
        => await syncService.SyncWorldCupAsync();

    public async Task<bool> DeleteMatchByApiIdAsync(int apiFootballId, [Service] ParisSpo.Domain.Interfaces.IMatchRepository repo)
    {
        await repo.DeleteByApiIdAsync(apiFootballId);
        return true;
    }
}
