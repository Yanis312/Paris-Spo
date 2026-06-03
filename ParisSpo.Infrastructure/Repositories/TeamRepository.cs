using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly IMongoCollection<Team> _collection;

    public TeamRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Team>("teams");
    }

    public async Task<Team?> GetByIdAsync(string id)
        => await _collection.Find(t => t.Id == id).FirstOrDefaultAsync();

    public async Task<Team?> GetByApiFootballIdAsync(int apiId)
        => await _collection.Find(t => t.ApiFootballId == apiId).FirstOrDefaultAsync();

    public async Task<List<Team>> GetByCompetitionAsync(Competition competition)
        => await _collection.Find(t => t.Competition == competition).ToListAsync();

    public async Task UpsertAsync(Team team)
    {
        team.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(
            t => t.ApiFootballId == team.ApiFootballId,
            team,
            new ReplaceOptions { IsUpsert = true });
    }
}
