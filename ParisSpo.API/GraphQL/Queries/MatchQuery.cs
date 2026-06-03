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
}
