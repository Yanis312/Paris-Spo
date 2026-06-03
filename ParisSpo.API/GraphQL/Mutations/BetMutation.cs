using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.API.GraphQL.Mutations;

[MutationType]
public class BetMutation
{
    public async Task<Bet> PlaceBetAsync(
        PlaceBetInput input,
        [Service] IBetRepository betRepo,
        [Service] IBankrollRepository bankrollRepo)
    {
        var bet = new Bet
        {
            Type = input.Selections.Count > 1 ? BetType.Combo : BetType.Single,
            Selections = input.Selections,
            Stake = input.Stake,
            TotalOdds = input.Selections.Aggregate(1.0, (acc, s) => acc * s.Odds),
            Bookmaker = input.Bookmaker,
            Notes = input.Notes,
            WasAiSuggested = input.WasAiSuggested
        };
        bet.PotentialReturn = bet.Stake * bet.TotalOdds;

        await betRepo.CreateAsync(bet);

        var bankroll = await bankrollRepo.GetAsync();
        if (bankroll != null)
        {
            var tx = new BankrollTransaction
            {
                BetId = bet.Id,
                Amount = -input.Stake,
                BalanceAfter = bankroll.CurrentAmount - input.Stake,
                Description = $"Pari placé — {bet.Type}"
            };
            await bankrollRepo.AddTransactionAsync(tx);
        }

        return bet;
    }

    public async Task<Bet> SettleBetAsync(
        string betId,
        BetStatus result,
        [Service] IBetRepository betRepo,
        [Service] IBankrollRepository bankrollRepo)
    {
        var bet = await betRepo.GetByIdAsync(betId)
            ?? throw new GraphQLException($"Bet {betId} not found");

        bet.Status = result;
        bet.SettledAt = DateTime.UtcNow;

        if (result == BetStatus.Won)
        {
            bet.ActualReturn = bet.Stake * bet.TotalOdds;
            var bankroll = await bankrollRepo.GetAsync();
            if (bankroll != null)
            {
                var tx = new BankrollTransaction
                {
                    BetId = bet.Id,
                    Amount = bet.ActualReturn.Value,
                    BalanceAfter = bankroll.CurrentAmount + bet.ActualReturn.Value,
                    Description = $"Pari gagné — retour {bet.ActualReturn:F2}$"
                };
                await bankrollRepo.AddTransactionAsync(tx);
            }
        }
        else if (result == BetStatus.Lost)
        {
            bet.ActualReturn = 0;
        }

        await betRepo.UpdateAsync(bet);
        return bet;
    }

    public async Task<Bankroll> InitializeBankrollAsync(
        double amount,
        [Service] IBankrollRepository repo)
        => await repo.InitializeAsync(amount);
}

public record PlaceBetInput(
    List<BetSelection> Selections,
    double Stake,
    string? Bookmaker,
    string? Notes,
    bool WasAiSuggested = false);
