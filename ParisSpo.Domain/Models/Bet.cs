using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParisSpo.Domain.Enums;

namespace ParisSpo.Domain.Models;

public class Bet
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public BetType Type { get; set; }
    public BetStatus Status { get; set; } = BetStatus.Pending;
    public List<BetSelection> Selections { get; set; } = [];
    public double Stake { get; set; }
    public double TotalOdds { get; set; }
    public double PotentialReturn { get; set; }
    public double? ActualReturn { get; set; }
    public string? Bookmaker { get; set; }
    public string? Notes { get; set; }
    public bool WasAiSuggested { get; set; }
    public double? AiConfidenceAtTime { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }
}

public class BetSelection
{
    public string MatchId { get; set; } = string.Empty;
    public string MatchDescription { get; set; } = string.Empty;
    public MarketType Market { get; set; }
    public string Pick { get; set; } = string.Empty; // ex: "Home", "Over 2.5", "BTTS Yes"
    public double Odds { get; set; }
    public BetStatus Status { get; set; } = BetStatus.Pending;
}
