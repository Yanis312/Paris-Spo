using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.API.GraphQL.Queries;

[QueryType]
public class MatchQuery
{
    public async Task<List<Match>> GetTodayMatchesAsync([Service] IMatchRepository repo)
        => await repo.GetTodayMatchesAsync();

    public async Task<List<Match>> GetMatchesByDateAsync(DateTime date, [Service] IMatchRepository repo)
        => await repo.GetMatchesByDateAsync(date);

    public async Task<Match?> GetMatchAsync(string id, [Service] IMatchRepository repo)
        => await repo.GetByIdAsync(id);

    public async Task<List<Match>> GetUpcomingMatchesAsync([Service] IMatchRepository repo)
        => await repo.GetUpcomingAsync(60);

    public async Task<List<Match>> GetAllMatchesAsync([Service] IMatchRepository repo)
        => await repo.GetAllAsync();
}
