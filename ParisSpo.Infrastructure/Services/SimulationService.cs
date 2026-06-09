using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.Infrastructure.Services;

/// <summary>
/// Simule une journée de paris : alloue un budget quotidien sur les value bets
/// détectés (allocation proportionnelle à Kelly), calcule l'espérance de gain,
/// et le résultat réel si les matchs sont terminés.
/// </summary>
public class SimulationService
{
    private readonly IMatchRepository _matchRepo;

    public SimulationService(IMatchRepository matchRepo) => _matchRepo = matchRepo;

    public async Task<DailySimulation> SimulateDayAsync(double dailyBudget, DateTime date)
    {
        var matches = await _matchRepo.GetMatchesByDateAsync(date);
        return Simulate(matches, dailyBudget, date);
    }

    public async Task<DailySimulation> SimulateTodayAsync(double dailyBudget)
    {
        var matches = await _matchRepo.GetTodayMatchesAsync();
        return Simulate(matches, dailyBudget, DateTime.UtcNow.Date);
    }

    private static DailySimulation Simulate(List<Match> matches, double budget, DateTime date)
    {
        var sim = new DailySimulation
        {
            Date = date,
            DailyBudget = budget,
            TotalMatches = matches.Count,
        };

        // Collecte tous les value bets du jour
        var candidates = new List<SimulatedBet>();
        foreach (var m in matches)
        {
            var a = m.AiAnalysis;
            if (a == null) continue;

            foreach (var s in a.Suggestions.Where(x => x.IsValueBet && x.KellyFraction > 0))
            {
                var prob = SideProbability(a, s.Description, m);
                candidates.Add(new SimulatedBet
                {
                    MatchId = m.Id,
                    Match = $"{m.HomeTeamName} vs {m.AwayTeamName}",
                    Competition = m.CompetitionName,
                    Pick = s.Description,
                    PickSide = SideOf(s.Description, m),
                    Odds = s.BookmakerOdds,
                    Probability = prob,
                    ValueEdge = s.ValueEdge,
                    KellyFraction = s.KellyFraction,
                });
            }
        }

        sim.ValueBetCount = candidates.Count;
        if (candidates.Count == 0) return sim;

        // Allocation du budget proportionnelle à Kelly (normalisée)
        var kellySum = candidates.Sum(c => c.KellyFraction);
        foreach (var bet in candidates)
        {
            bet.Stake = Math.Round(budget * (bet.KellyFraction / kellySum), 2);
            bet.PotentialReturn = Math.Round(bet.Stake * bet.Odds, 2);
            bet.ExpectedValue = Math.Round(bet.Probability * bet.PotentialReturn - bet.Stake, 2);
        }

        sim.Bets = candidates;
        sim.TotalStaked = Math.Round(candidates.Sum(c => c.Stake), 2);
        sim.ExpectedReturn = Math.Round(candidates.Sum(c => c.Probability * c.PotentialReturn), 2);
        sim.ExpectedProfit = Math.Round(sim.ExpectedReturn - sim.TotalStaked, 2);

        // Résultat réel si matchs finis
        foreach (var bet in candidates)
        {
            var match = matches.First(m => m.Id == bet.MatchId);
            if (match.Status == MatchStatus.Finished && match.Score != null)
            {
                var won = DidWin(bet.PickSide, match.Score);
                bet.Result = won ? "WON" : "LOST";
                bet.ActualReturn = won ? bet.PotentialReturn : 0;
                sim.HasResults = true;
            }
        }

        if (sim.HasResults)
        {
            var played = candidates.Where(b => b.Result != null).ToList();
            sim.ActualReturn = Math.Round(played.Sum(b => b.ActualReturn), 2);
            sim.ActualProfit = Math.Round(sim.ActualReturn - played.Sum(b => b.Stake), 2);
        }

        return sim;
    }

    private static double SideProbability(AiAnalysis a, string desc, Match m)
    {
        var side = SideOf(desc, m);
        return side switch
        {
            "Home" => a.HomeWinProbability,
            "Draw" => a.DrawProbability,
            "Away" => a.AwayWinProbability,
            _ => 0
        };
    }

    private static string SideOf(string desc, Match m)
    {
        if (desc.Contains(m.HomeTeamName)) return "Home";
        if (desc.Contains(m.AwayTeamName)) return "Away";
        if (desc.Contains("nul", StringComparison.OrdinalIgnoreCase)) return "Draw";
        return "Home";
    }

    private static bool DidWin(string side, MatchScore score) => side switch
    {
        "Home" => score.HomeGoals > score.AwayGoals,
        "Away" => score.AwayGoals > score.HomeGoals,
        "Draw" => score.HomeGoals == score.AwayGoals,
        _ => false
    };
}
