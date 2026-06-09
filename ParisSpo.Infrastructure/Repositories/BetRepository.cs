using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.Repositories;

public class BetRepository : IBetRepository
{
    private readonly IMongoCollection<Bet> _collection;

    public BetRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Bet>("bets");
    }

    public async Task<List<Bet>> GetAllAsync()
        => await _collection.Find(_ => true).SortByDescending(b => b.PlacedAt).ToListAsync();

    public async Task<List<Bet>> GetByStatusAsync(BetStatus status)
        => await _collection.Find(b => b.Status == status).SortByDescending(b => b.PlacedAt).ToListAsync();

    public async Task<Bet?> GetByIdAsync(string id)
        => await _collection.Find(b => b.Id == id).FirstOrDefaultAsync();

    public async Task<string> CreateAsync(Bet bet)
    {
        await _collection.InsertOneAsync(bet);
        return bet.Id;
    }

    public async Task UpdateAsync(Bet bet)
        => await _collection.ReplaceOneAsync(b => b.Id == bet.Id, bet);

    public async Task DeleteAsync(string id)
        => await _collection.DeleteOneAsync(b => b.Id == id);

    public async Task<long> DeletePendingAsync()
    {
        var res = await _collection.DeleteManyAsync(b => b.Status == BetStatus.Pending);
        return res.DeletedCount;
    }

    public async Task<BetStats> GetStatsAsync()
    {
        var all = await GetAllAsync();
        var settled = all.Where(b => b.Status is BetStatus.Won or BetStatus.Lost).ToList();

        return new BetStats
        {
            TotalBets = settled.Count,
            Won = settled.Count(b => b.Status == BetStatus.Won),
            Lost = settled.Count(b => b.Status == BetStatus.Lost),
            Pending = all.Count(b => b.Status == BetStatus.Pending),
            TotalStaked = settled.Sum(b => b.Stake),
            TotalReturned = settled.Sum(b => b.ActualReturn ?? 0)
        };
    }
}
