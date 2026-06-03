namespace ParisSpo.Domain.Enums;

public enum BetStatus
{
    Pending,
    Won,
    Lost,
    Void,
    CashOut
}

public enum BetType
{
    Single,
    Combo,
    System
}

public enum MarketType
{
    MatchWinner,        // 1X2
    BothTeamsScore,     // BTTS
    OverUnder,          // Over/Under 2.5
    AsianHandicap,
    CorrectScore,
    FirstGoalScorer,
    HalfTimeResult,
    DoubleChance
}

public enum MatchStatus
{
    Scheduled,
    Live,
    Finished,
    Postponed,
    Cancelled
}

public enum Competition
{
    PremierLeague,
    Ligue1,
    LaLiga,
    SerieA,
    Bundesliga,
    ChampionsLeague,
    EuropaLeague,
    WorldCup,
    EuroCup
}
