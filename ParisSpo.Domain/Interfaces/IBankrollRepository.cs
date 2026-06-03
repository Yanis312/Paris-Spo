using ParisSpo.Domain.Models;

namespace ParisSpo.Domain.Interfaces;

public interface IBankrollRepository
{
    Task<Bankroll?> GetAsync();
    Task<Bankroll> InitializeAsync(double initialAmount);
    Task UpdateAsync(Bankroll bankroll);
    Task AddTransactionAsync(BankrollTransaction transaction);
}
