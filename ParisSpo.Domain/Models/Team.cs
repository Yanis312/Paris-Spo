using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParisSpo.Domain.Enums;

namespace ParisSpo.Domain.Models;

public class Team
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public int ApiFootballId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public Competition Competition { get; set; }
    public TeamForm Form { get; set; } = new();
    public List<PlayerInjury> Injuries { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TeamForm
{
    public string Last5 { get; set; } = string.Empty; // ex: "WWDLW"
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public double AverageGoalsScored { get; set; }
    public double AverageGoalsConceded { get; set; }
    public int CleanSheets { get; set; }
    public int BothTeamsScoredCount { get; set; }
}

public class PlayerInjury
{
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string InjuryType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? ExpectedReturn { get; set; }
    public bool IsImportantPlayer { get; set; }
}
