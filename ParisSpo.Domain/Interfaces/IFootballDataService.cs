using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Models;

namespace ParisSpo.Domain.Interfaces;

public interface IFootballDataService
{
    Task<List<Match>> GetTodayMatchesAsync();
    Task<List<Match>> GetMatchesByDateAsync(DateTime date);
    Task<List<Match>> GetMatchesByCompetitionAsync(Competition competition, DateTime dateFrom, DateTime dateTo);
    Task<Team?> GetTeamAsync(int teamId);
    Task<List<PlayerInjury>> GetInjuriesAsync(int teamId);
}
