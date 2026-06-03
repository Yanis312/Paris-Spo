using ParisSpo.Domain.Models;
using ParisSpo.Domain.Enums;

namespace ParisSpo.Domain.Interfaces;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id);
    Task<Team?> GetByApiFootballIdAsync(int apiId);
    Task<List<Team>> GetByCompetitionAsync(Competition competition);
    Task UpsertAsync(Team team);
}
