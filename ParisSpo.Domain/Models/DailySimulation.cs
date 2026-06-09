namespace ParisSpo.Domain.Models;

public class DailySimulation
{
    public DateTime Date { get; set; }
    public double DailyBudget { get; set; }
    public int TotalMatches { get; set; }
    public int ValueBetCount { get; set; }
    public double TotalStaked { get; set; }
    public double ExpectedReturn { get; set; }      // espérance mathématique
    public double ExpectedProfit { get; set; }      // EV − mise
    public double ActualReturn { get; set; }        // si matchs finis
    public double ActualProfit { get; set; }
    public bool HasResults { get; set; }            // true si au moins 1 match fini
    public List<SimulatedBet> Bets { get; set; } = [];
}

public class SimulatedBet
{
    public string MatchId { get; set; } = string.Empty;
    public string Match { get; set; } = string.Empty;
    public string Competition { get; set; } = string.Empty;
    public string Pick { get; set; } = string.Empty;     // ex "Victoire France"
    public string PickSide { get; set; } = string.Empty; // Home/Draw/Away
    public double Odds { get; set; }
    public double Probability { get; set; }              // proba estimée (blend)
    public double ValueEdge { get; set; }                // %
    public double KellyFraction { get; set; }
    public double Stake { get; set; }                    // mise allouée
    public double PotentialReturn { get; set; }          // si gagné
    public double ExpectedValue { get; set; }            // EV de ce pari
    public string? Result { get; set; }                  // "WON" / "LOST" / null si pas joué
    public double ActualReturn { get; set; }
}
