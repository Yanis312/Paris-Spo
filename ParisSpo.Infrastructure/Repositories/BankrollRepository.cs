using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.Repositories;

public class BankrollRepository : IBankrollRepository
{
    private readonly IMongoCollection<Bankroll> _collection;

    public BankrollRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Bankroll>("bankroll");
    }

    public async Task<Bankroll?> GetAsync()
        => await _collection.Find(_ => true).FirstOrDefaultAsync();

    public async Task<Bankroll> InitializeAsync(double initialAmount)
    {
        var bankroll = new Bankroll
        {
            InitialAmount = initialAmount,
            CurrentAmount = initialAmount
        };
        await _collection.InsertOneAsync(bankroll);
        return bankroll;
    }

    public async Task UpdateAsync(Bankroll bankroll)
    {
        bankroll.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(b => b.Id == bankroll.Id, bankroll);
    }

    public async Task AddTransactionAsync(BankrollTransaction transaction)
    {
        var update = Builders<Bankroll>.Update
            .Push(b => b.Transactions, transaction)
            .Set(b => b.CurrentAmount, transaction.BalanceAfter)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(_ => true, update);
    }
}
