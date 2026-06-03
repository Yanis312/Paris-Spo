using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.API.GraphQL.Queries;

[QueryType]
public class BetQuery
{
    public async Task<List<Bet>> GetBetsAsync([Service] IBetRepository repo)
        => await repo.GetAllAsync();

    public async Task<List<Bet>> GetBetsByStatusAsync(BetStatus status, [Service] IBetRepository repo)
        => await repo.GetByStatusAsync(status);

    public async Task<Bet?> GetBetAsync(string id, [Service] IBetRepository repo)
        => await repo.GetByIdAsync(id);

    public async Task<BetStats> GetBetStatsAsync([Service] IBetRepository repo)
        => await repo.GetStatsAsync();
}
