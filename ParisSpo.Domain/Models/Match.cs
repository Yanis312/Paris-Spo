using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParisSpo.Domain.Enums;

namespace ParisSpo.Domain.Models;

public class Match
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public int ApiFootballId { get; set; }
    public string HomeTeamId { get; set; } = string.Empty;
    public string AwayTeamId { get; set; } = string.Empty;
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public Competition Competition { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public DateTime KickOff { get; set; }
    public MatchStatus Status { get; set; }
    public MatchScore? Score { get; set; }
    public List<MatchOdds> Odds { get; set; } = [];
    public AiAnalysis? AiAnalysis { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MatchScore
{
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public int? HomeGoalsHT { get; set; }
    public int? AwayGoalsHT { get; set; }
}

public class MatchOdds
{
    public string Bookmaker { get; set; } = string.Empty;
    public double HomeWin { get; set; }
    public double Draw { get; set; }
    public double AwayWin { get; set; }
    public double? Over25 { get; set; }
    public double? Under25 { get; set; }
    public double? BttsYes { get; set; }
    public double? BttsNo { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}

public class AiAnalysis
{
    public double HomeWinProbability { get; set; }
    public double DrawProbability { get; set; }
    public double AwayWinProbability { get; set; }
    public double ConfidenceScore { get; set; } // 0-100
    public List<BetSuggestion> Suggestions { get; set; } = [];
    public string ForumSentiment { get; set; } = string.Empty;
    public string InjuryImpact { get; set; } = string.Empty;
    public string AnalysisSummary { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class BetSuggestion
{
    public MarketType Market { get; set; }
    public string Description { get; set; } = string.Empty;
    public double TrueOdds { get; set; }        // probabilité réelle convertie en cote
    public double BookmakerOdds { get; set; }   // meilleure cote trouvée
    public double ValueEdge { get; set; }       // % d'avantage vs bookmaker
    public double KellyFraction { get; set; }   // % bankroll recommandé
    public bool IsValueBet { get; set; }        // true si ValueEdge > seuil
    public string Bookmaker { get; set; } = string.Empty;
}
