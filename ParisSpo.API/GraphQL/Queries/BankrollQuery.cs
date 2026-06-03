using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.API.GraphQL.Queries;

[QueryType]
public class BankrollQuery
{
    public async Task<Bankroll?> GetBankrollAsync([Service] IBankrollRepository repo)
        => await repo.GetAsync();
}
