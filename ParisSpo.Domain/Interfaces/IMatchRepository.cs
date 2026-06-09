using ParisSpo.Domain.Models;

namespace ParisSpo.Domain.Interfaces;

public interface IMatchRepository
{
    Task<List<Match>> GetTodayMatchesAsync();
    Task<List<Match>> GetMatchesByDateAsync(DateTime date);
    Task<Match?> GetByIdAsync(string id);
    Task<Match?> GetByApiFootballIdAsync(int apiId);
    Task UpsertAsync(Match match);
    Task<List<Match>> GetRecentFinishedAsync(int count = 20);
    Task<List<Match>> GetUpcomingAsync(int days = 60);
    Task<List<Match>> GetAllAsync();
    Task DeleteByApiIdAsync(int apiFootballId);
}
