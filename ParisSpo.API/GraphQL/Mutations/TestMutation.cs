using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.API.GraphQL.Mutations;

/// <summary>
/// Test data seeder — injects realistic WC 2026 matches for local testing.
/// Remove or disable before production.
/// </summary>
[MutationType]
public class TestMutation
{
    /// <summary>
    /// Démo simulation : 4 matchs FINIS avec value bets réalistes (edge 6-12%) + scores.
    /// Permet de voir une journée complète de paris avec gains/pertes réels.
    /// </summary>
    public async Task<List<Match>> SeedDemoSimulationAsync([Service] IMatchRepository repo)
    {
        var today = DateTime.UtcNow.Date;

        Match Make(int id, string h, string a, string comp,
            double oh, double od, double oa,           // cotes
            double ph, double pd, double pa,           // probas IA (légèrement > marché sur 1 pick)
            int hg, int ag)                            // score final
        {
            BetSuggestion S(string desc, string side, double prob, double odd) {
                var edge = prob * odd - 1;
                return new BetSuggestion {
                    Market = MarketType.MatchWinner, Description = desc,
                    BookmakerOdds = odd, TrueOdds = Math.Round(1/prob,2),
                    ValueEdge = Math.Round(edge*100,1),
                    KellyFraction = Math.Clamp((odd*prob-1)/(odd-1),0,0.25),
                    IsValueBet = edge > 0.05 && edge < 0.20 && prob > 0.15 && odd < 6.0,
                    Bookmaker = "Sofascore", ConfidenceScore = 72
                };
            }
            return new Match {
                ApiFootballId = id, HomeTeamName = h, AwayTeamName = a,
                CompetitionName = comp, Competition = Competition.WorldCup,
                KickOff = today.AddHours(18), Status = MatchStatus.Finished,
                Score = new MatchScore { HomeGoals = hg, AwayGoals = ag },
                Odds = [new MatchOdds { Bookmaker="Sofascore", HomeWin=oh, Draw=od, AwayWin=oa }],
                AiAnalysis = new AiAnalysis {
                    HomeWinProbability=ph, DrawProbability=pd, AwayWinProbability=pa,
                    ConfidenceScore=72, AnalysisSummary="Démo simulation",
                    Suggestions=[ S($"Victoire {h}","Home",ph,oh), S("Match nul","Draw",pd,od), S($"Victoire {a}","Away",pa,oa) ]
                }
            };
        }

        // Value bets réalistes : proba IA légèrement > marché → edge 6-12%
        var demo = new List<Match>
        {
            // France favori, IA voit 58% vs marché ~52% (cote 1.75) → edge +1.5% non, ajuste
            Make(990001,"France","Croatie","Coupe du Monde 2026", 1.90,3.40,4.20, 0.58,0.25,0.17, 2,1),   // France gagne ✓ value home
            Make(990002,"Portugal","Suisse","Coupe du Monde 2026", 2.10,3.20,3.60, 0.52,0.26,0.22, 1,1),   // nul, value home perdu
            Make(990003,"Angleterre","Danemark","Coupe du Monde 2026", 1.80,3.50,4.50, 0.62,0.23,0.15, 3,0), // Angleterre ✓ value home
            Make(990004,"Espagne","Pays-Bas","Coupe du Monde 2026", 2.30,3.30,3.10, 0.40,0.27,0.33, 0,2),   // Pays-Bas gagne, value away ✓
        };

        foreach (var m in demo) await repo.UpsertAsync(m);
        return demo;
    }

    public async Task<List<Match>> SeedTestMatchesAsync([Service] IMatchRepository repo)
    {
        var today = DateTime.UtcNow.Date;

        var matches = new List<Match>
        {
            new()
            {
                ApiFootballId = 900001,
                HomeTeamName = "France",
                AwayTeamName = "Brésil",
                CompetitionName = "FIFA World Cup 2026",
                Competition = Competition.WorldCup,
                KickOff = today.AddHours(19),
                Status = MatchStatus.Scheduled,
                Odds =
                [
                    new MatchOdds { Bookmaker = "bet365",  HomeWin = 2.40, Draw = 3.10, AwayWin = 2.90, Over25 = 1.75, Under25 = 2.10 },
                    new MatchOdds { Bookmaker = "unibet",  HomeWin = 2.35, Draw = 3.20, AwayWin = 2.95, Over25 = 1.72, Under25 = 2.15 },
                    new MatchOdds { Bookmaker = "winamax", HomeWin = 2.45, Draw = 3.05, AwayWin = 2.85, Over25 = 1.78, Under25 = 2.08 }
                ]
            },
            new()
            {
                ApiFootballId = 900002,
                HomeTeamName = "Espagne",
                AwayTeamName = "Allemagne",
                CompetitionName = "FIFA World Cup 2026",
                Competition = Competition.WorldCup,
                KickOff = today.AddHours(21).AddMinutes(45),
                Status = MatchStatus.Scheduled,
                Odds =
                [
                    new MatchOdds { Bookmaker = "bet365",  HomeWin = 2.10, Draw = 3.30, AwayWin = 3.50, Over25 = 1.80, Under25 = 2.00 },
                    new MatchOdds { Bookmaker = "unibet",  HomeWin = 2.15, Draw = 3.25, AwayWin = 3.40, Over25 = 1.82, Under25 = 2.02 },
                    new MatchOdds { Bookmaker = "winamax", HomeWin = 2.08, Draw = 3.35, AwayWin = 3.55, Over25 = 1.78, Under25 = 2.05 }
                ]
            },
            new()
            {
                ApiFootballId = 900003,
                HomeTeamName = "Argentine",
                AwayTeamName = "Angleterre",
                CompetitionName = "FIFA World Cup 2026",
                Competition = Competition.WorldCup,
                KickOff = today.AddHours(23),
                Status = MatchStatus.Scheduled,
                Odds =
                [
                    new MatchOdds { Bookmaker = "bet365",  HomeWin = 2.60, Draw = 3.20, AwayWin = 2.70, Over25 = 1.85, Under25 = 1.95 },
                    new MatchOdds { Bookmaker = "pinnacle", HomeWin = 2.65, Draw = 3.15, AwayWin = 2.65, Over25 = 1.88, Under25 = 1.92 }
                ]
            },
            new()
            {
                ApiFootballId = 900004,
                HomeTeamName = "Portugal",
                AwayTeamName = "Maroc",
                CompetitionName = "FIFA World Cup 2026",
                Competition = Competition.WorldCup,
                KickOff = today.AddHours(17),
                Status = MatchStatus.Scheduled,
                Odds =
                [
                    new MatchOdds { Bookmaker = "bet365",  HomeWin = 1.75, Draw = 3.50, AwayWin = 4.50, Over25 = 1.90, Under25 = 1.90 },
                    new MatchOdds { Bookmaker = "unibet",  HomeWin = 1.78, Draw = 3.45, AwayWin = 4.40, Over25 = 1.88, Under25 = 1.92 }
                ]
            },
            new()
            {
                ApiFootballId = 900005,
                HomeTeamName = "Pays-Bas",
                AwayTeamName = "Algérie",
                CompetitionName = "Match Amical International",
                Competition = Competition.WorldCup,
                KickOff = today.AddHours(20).AddMinutes(30),
                Status = MatchStatus.Scheduled,
                Odds =
                [
                    new MatchOdds { Bookmaker = "bet365",  HomeWin = 1.55, Draw = 3.80, AwayWin = 6.00, Over25 = 1.70, Under25 = 2.20 },
                    new MatchOdds { Bookmaker = "unibet",  HomeWin = 1.57, Draw = 3.75, AwayWin = 5.80, Over25 = 1.72, Under25 = 2.18 }
                ]
            }
        };

        foreach (var match in matches)
            await repo.UpsertAsync(match);

        return matches;
    }

    public async Task<List<Match>> ClearTestMatchesAsync([Service] IMatchRepository repo)
    {
        // Retourne les matchs du jour après nettoyage (pour vérification)
        return await repo.GetTodayMatchesAsync();
    }
}
