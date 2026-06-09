using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly IMongoCollection<Match> _collection;

    public MatchRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Match>("matches");

        var indexKeys = Builders<Match>.IndexKeys.Ascending(m => m.KickOff);
        _collection.Indexes.CreateOne(new CreateIndexModel<Match>(indexKeys));
    }

    public async Task<List<Match>> GetTodayMatchesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return await _collection
            .Find(m => m.KickOff >= today && m.KickOff < tomorrow)
            .SortBy(m => m.KickOff)
            .ToListAsync();
    }

    public async Task<List<Match>> GetMatchesByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return await _collection
            .Find(m => m.KickOff >= start && m.KickOff < end)
            .SortBy(m => m.KickOff)
            .ToListAsync();
    }

    public async Task<Match?> GetByIdAsync(string id)
        => await _collection.Find(m => m.Id == id).FirstOrDefaultAsync();

    public async Task<Match?> GetByApiFootballIdAsync(int apiId)
        => await _collection.Find(m => m.ApiFootballId == apiId).FirstOrDefaultAsync();

    public async Task UpsertAsync(Match match)
    {
        match.UpdatedAt = DateTime.UtcNow;

        // Préserve l'_id existant (immutable) — sinon ReplaceOne tente de l'altérer
        var existing = await _collection
            .Find(m => m.ApiFootballId == match.ApiFootballId)
            .FirstOrDefaultAsync();

        if (existing != null)
            match.Id = existing.Id;

        await _collection.ReplaceOneAsync(
            m => m.ApiFootballId == match.ApiFootballId,
            match,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task<List<Match>> GetRecentFinishedAsync(int count = 20)
        => await _collection
            .Find(m => m.Status == Domain.Enums.MatchStatus.Finished)
            .SortByDescending(m => m.KickOff)
            .Limit(count)
            .ToListAsync();

    public async Task<List<Match>> GetUpcomingAsync(int days = 60)
    {
        var now = DateTime.UtcNow.Date;
        var until = now.AddDays(days);
        return await _collection
            .Find(m => m.KickOff >= now && m.KickOff < until)
            .SortBy(m => m.KickOff)
            .ToListAsync();
    }

    public async Task<List<Match>> GetAllAsync()
        => await _collection.Find(_ => true).SortBy(m => m.KickOff).ToListAsync();

    public async Task DeleteByApiIdAsync(int apiFootballId)
        => await _collection.DeleteOneAsync(m => m.ApiFootballId == apiFootballId);
}
