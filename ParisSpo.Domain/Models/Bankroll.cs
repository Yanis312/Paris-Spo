using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParisSpo.Domain.Models;

public class Bankroll
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public double InitialAmount { get; set; }
    public double CurrentAmount { get; set; }
    public double MaxStakePercent { get; set; } = 5.0;  // % max par pari
    public double KellyFraction { get; set; } = 0.25;   // Kelly fractionné (prudent)
    public List<BankrollTransaction> Transactions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public double Roi => InitialAmount == 0 ? 0
        : (CurrentAmount - InitialAmount) / InitialAmount * 100;

    public double MaxRecommendedStake => CurrentAmount * (MaxStakePercent / 100);
}

public class BankrollTransaction
{
    public string? BetId { get; set; }
    public double Amount { get; set; }        // positif = gain, négatif = perte
    public double BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
