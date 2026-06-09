using ParisSpo.Domain.Models;
using ParisSpo.Domain.Enums;

namespace ParisSpo.Domain.Interfaces;

public interface IBetRepository
{
    Task<List<Bet>> GetAllAsync();
    Task<List<Bet>> GetByStatusAsync(BetStatus status);
    Task<Bet?> GetByIdAsync(string id);
    Task<string> CreateAsync(Bet bet);
    Task UpdateAsync(Bet bet);
    Task DeleteAsync(string id);
    Task<long> DeletePendingAsync();
    Task<BetStats> GetStatsAsync();
}

public class BetStats
{
    public int TotalBets { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public int Pending { get; set; }
    public double TotalStaked { get; set; }
    public double TotalReturned { get; set; }
    public double Roi => TotalStaked == 0 ? 0 : (TotalReturned - TotalStaked) / TotalStaked * 100;
    public double WinRate => TotalBets == 0 ? 0 : (double)Won / (Won + Lost) * 100;
}
