using ParisSpo.Domain.Models;

namespace ParisSpo.Domain.Interfaces;

public interface IOddsService
{
    Task<List<MatchOdds>> GetOddsForMatchAsync(string homeTeam, string awayTeam, DateTime kickOff);
    Task<List<OddsEvent>> GetUpcomingOddsAsync();
}

public class OddsEvent
{
    public string Id { get; set; } = string.Empty;
    public string SportKey { get; set; } = string.Empty;
    public DateTime CommenceTime { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public List<MatchOdds> Odds { get; set; } = [];
}
