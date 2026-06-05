using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.AI.Services;

public class MatchAnalysisAgent : IAiAnalysisService
{
    private readonly FallbackKernelExecutor _executor;
    private readonly ILogger<MatchAnalysisAgent> _logger;

    public MatchAnalysisAgent(FallbackKernelExecutor executor, ILogger<MatchAnalysisAgent> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task<AiAnalysis> AnalyzeMatchAsync(Match match)
    {
        var prompt = BuildMatchPrompt(match);

        _logger.LogInformation("Analyzing {Home} vs {Away}", match.HomeTeamName, match.AwayTeamName);

        var rawResponse = await _executor.ExecuteAsync(prompt);

        return ParseAnalysis(rawResponse, match);
    }

    public async Task<ComboEvaluation> EvaluateComboAsync(List<BetSuggestion> selections)
    {
        var prompt = BuildComboPrompt(selections);
        var rawResponse = await _executor.ExecuteAsync(prompt);
        return ParseComboEvaluation(rawResponse, selections);
    }

    private static string BuildMatchPrompt(Match match)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Tu es un expert en analyse de paris sportifs football. Analyse ce match et réponds en JSON uniquement.");
        sb.AppendLine();
        sb.AppendLine($"Match: {match.HomeTeamName} vs {match.AwayTeamName}");
        sb.AppendLine($"Compétition: {match.CompetitionName}");
        sb.AppendLine($"Date/heure: {match.KickOff:dd/MM/yyyy HH:mm} UTC");

        // Cotes disponibles
        if (match.Odds.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Cotes bookmakers:");
            foreach (var odds in match.Odds)
            {
                sb.AppendLine($"  {odds.Bookmaker}: 1={odds.HomeWin:F2} X={odds.Draw:F2} 2={odds.AwayWin:F2}" +
                              (odds.Over25.HasValue ? $" Over2.5={odds.Over25:F2}" : ""));
            }
        }

        // Blessures
        if (match.Odds.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("(Données blessures non disponibles sur tier gratuit — base ton analyse sur les cotes et ta connaissance des équipes)");
        }

        sb.AppendLine();
        sb.AppendLine("Réponds UNIQUEMENT avec ce JSON (pas de texte avant/après):");
        sb.AppendLine("""
{
  "homeWinProbability": 0.45,
  "drawProbability": 0.28,
  "awayWinProbability": 0.27,
  "confidenceScore": 72,
  "forumSentiment": "Résumé en 1 phrase du sentiment général des fans",
  "injuryImpact": "Impact des absences sur le match",
  "analysisSummary": "Résumé de l'analyse en 2-3 phrases",
  "suggestions": [
    {
      "market": "MATCH_WINNER",
      "description": "Victoire domicile",
      "trueOdds": 2.10,
      "bookmakerOdds": 2.30,
      "valueEdge": 9.5,
      "kellyFraction": 0.045,
      "isValueBet": true,
      "bookmaker": "bet365"
    }
  ]
}
""");
        sb.AppendLine("Les probabilités doivent sommer à 1.0 exactement.");
        sb.AppendLine("isValueBet = true seulement si bookmakerOdds > trueOdds (avantage réel détecté).");
        sb.AppendLine("valueEdge = (bookmakerOdds/trueOdds - 1) * 100");
        sb.AppendLine("kellyFraction = (bookmakerOdds * p - 1) / (bookmakerOdds - 1) où p = probabilité");

        return sb.ToString();
    }

    private static string BuildComboPrompt(List<BetSuggestion> selections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Évalue ce combiné de paris. Réponds UNIQUEMENT en JSON.");
        sb.AppendLine();
        sb.AppendLine("Sélections:");
        foreach (var s in selections)
            sb.AppendLine($"  - {s.Description} @ {s.BookmakerOdds:F2} (confiance individuelle: {s.ConfidenceScore:F0}%)");

        var combinedOdds = selections.Aggregate(1.0, (acc, s) => acc * s.BookmakerOdds);
        sb.AppendLine($"Cote combinée: {combinedOdds:F2}");
        sb.AppendLine();
        sb.AppendLine("""
Réponds:
{
  "combinedOdds": 5.50,
  "successProbability": 0.12,
  "expectedValue": -2.50,
  "riskLevel": "Élevé",
  "aiRecommendation": "Conseil en 1-2 phrases",
  "recommendedStake": 15.0
}
""");
        return sb.ToString();
    }

    private AiAnalysis ParseAnalysis(string raw, Match match)
    {
        try
        {
            // Extraire le JSON de la réponse (le modèle peut ajouter du texte autour)
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = raw[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<AiAnalysisDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed != null)
                    return MapToAnalysis(parsed, match);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse AI JSON response: {Error}", ex.Message);
        }

        // Fallback: analyse basée sur les cotes uniquement
        return BuildFallbackAnalysis(match, raw);
    }

    private static AiAnalysis MapToAnalysis(AiAnalysisDto dto, Match match)
    {
        // Normalise les probabilités IA pour sommer à 1.0
        var total = dto.HomeWinProbability + dto.DrawProbability + dto.AwayWinProbability;
        if (total <= 0) total = 1;

        var aiHome = dto.HomeWinProbability / total;
        var aiDraw = dto.DrawProbability / total;
        var aiAway = dto.AwayWinProbability / total;

        // Ancre les probas IA au marché (cotes) pour éviter hallucinations sur ligues obscures.
        // Marché = sagesse collective, IA = léger ajustement. Blend 70% marché / 30% IA.
        var (homeP, drawP, awayP) = BlendWithMarket(match, aiHome, aiDraw, aiAway);

        return new AiAnalysis
        {
            HomeWinProbability = homeP,
            DrawProbability = drawP,
            AwayWinProbability = awayP,
            ConfidenceScore = Math.Clamp(dto.ConfidenceScore, 0, 100),
            ForumSentiment = dto.ForumSentiment ?? "Sentiment non disponible",
            InjuryImpact = dto.InjuryImpact ?? "Données blessures non disponibles",
            AnalysisSummary = dto.AnalysisSummary ?? string.Empty,
            GeneratedAt = DateTime.UtcNow,
            // Suggestions CALCULÉES depuis vraies cotes match × probas IA (pas hallucinées par LLM)
            Suggestions = ComputeValueBets(match, homeP, drawP, awayP)
        };
    }

    /// <summary>
    /// Ancre les probabilités IA aux probabilités implicites du marché (cotes, marge retirée).
    /// Évite les hallucinations sur ligues obscures où l'IA ne connaît pas les équipes.
    /// Blend pondéré : 70% marché + 30% IA.
    /// </summary>
    private static (double home, double draw, double away) BlendWithMarket(
        Match match, double aiHome, double aiDraw, double aiAway)
    {
        var odds = match.Odds.FirstOrDefault();
        if (odds == null || odds.HomeWin <= 1 || odds.Draw <= 1 || odds.AwayWin <= 1)
            return (aiHome, aiDraw, aiAway); // pas de cotes → garde IA brute

        // Proba implicite = 1/cote, normalisée (retire la marge bookmaker)
        var rawH = 1.0 / odds.HomeWin;
        var rawD = 1.0 / odds.Draw;
        var rawA = 1.0 / odds.AwayWin;
        var margin = rawH + rawD + rawA;
        var mktH = rawH / margin;
        var mktD = rawD / margin;
        var mktA = rawA / margin;

        const double wMarket = 0.60, wAi = 0.40;
        var h = wMarket * mktH + wAi * aiHome;
        var d = wMarket * mktD + wAi * aiDraw;
        var a = wMarket * mktA + wAi * aiAway;
        var sum = h + d + a;
        return (h / sum, d / sum, a / sum);
    }

    /// <summary>
    /// Calcule les value bets de façon déterministe : compare proba IA aux VRAIES cotes du match.
    /// ValueEdge = proba_réelle × cote_bookmaker − 1. Positif = avantage mathématique.
    /// Kelly = (cote × p − 1) / (cote − 1).
    /// </summary>
    private static List<BetSuggestion> ComputeValueBets(Match match, double homeP, double drawP, double awayP)
    {
        var odds = match.Odds.FirstOrDefault();
        if (odds == null || odds.HomeWin <= 1) return [];

        var outcomes = new[]
        {
            (Desc: $"Victoire {match.HomeTeamName}", Pick: "Home", Prob: homeP, Odd: odds.HomeWin),
            (Desc: "Match nul", Pick: "Draw", Prob: drawP, Odd: odds.Draw),
            (Desc: $"Victoire {match.AwayTeamName}", Pick: "Away", Prob: awayP, Odd: odds.AwayWin),
        };

        var suggestions = new List<BetSuggestion>();
        foreach (var (desc, _, prob, odd) in outcomes)
        {
            if (odd <= 1) continue;

            var edge = prob * odd - 1;              // espérance par unité misée
            var kelly = (odd * prob - 1) / (odd - 1); // fraction Kelly

            suggestions.Add(new BetSuggestion
            {
                Market = MarketType.MatchWinner,
                Description = desc,
                TrueOdds = prob > 0 ? Math.Round(1 / prob, 2) : 0,
                BookmakerOdds = odd,                 // VRAIE cote du match
                ValueEdge = Math.Round(edge * 100, 1),
                KellyFraction = Math.Clamp(kelly, 0, 0.25),
                IsValueBet = edge > 0.05 && prob > 0.10, // min 5% edge + proba crédible
                Bookmaker = odds.Bookmaker
            });
        }

        // garde seulement la meilleure value (ou toutes les positives)
        return suggestions.OrderByDescending(s => s.ValueEdge).ToList();
    }

    private static AiAnalysis BuildFallbackAnalysis(Match match, string rawText)
    {
        // Calcul probabilités implicites depuis les cotes (enlève la marge bookmaker)
        var odds = match.Odds.FirstOrDefault();
        double homeP = 0.4, drawP = 0.27, awayP = 0.33;

        if (odds is { HomeWin: > 0, Draw: > 0, AwayWin: > 0 })
        {
            var rawHome = 1.0 / odds.HomeWin;
            var rawDraw = 1.0 / odds.Draw;
            var rawAway = 1.0 / odds.AwayWin;
            var margin = rawHome + rawDraw + rawAway;
            homeP = rawHome / margin;
            drawP = rawDraw / margin;
            awayP = rawAway / margin;
        }

        return new AiAnalysis
        {
            HomeWinProbability = homeP,
            DrawProbability = drawP,
            AwayWinProbability = awayP,
            ConfidenceScore = 45, // faible — fallback mécanique
            ForumSentiment = "Analyse basée sur les cotes uniquement",
            InjuryImpact = "Non analysé",
            AnalysisSummary = string.IsNullOrWhiteSpace(rawText)
                ? "Analyse IA indisponible — probabilités calculées depuis les cotes bookmaker."
                : rawText,
            GeneratedAt = DateTime.UtcNow,
            Suggestions = []
        };
    }

    private ComboEvaluation ParseComboEvaluation(string raw, List<BetSuggestion> selections)
    {
        try
        {
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = raw[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<ComboEvaluationDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null)
                    return new ComboEvaluation
                    {
                        CombinedOdds = parsed.CombinedOdds,
                        SuccessProbability = parsed.SuccessProbability,
                        ExpectedValue = parsed.ExpectedValue,
                        RiskLevel = parsed.RiskLevel ?? "Inconnu",
                        AiRecommendation = parsed.AiRecommendation ?? raw,
                        RecommendedStake = parsed.RecommendedStake
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse combo JSON: {Error}", ex.Message);
        }

        // Fallback mécanique
        var combinedOdds = selections.Aggregate(1.0, (acc, s) => acc * s.BookmakerOdds);
        var prob = selections.Aggregate(1.0, (acc, s) => acc * (1.0 / s.BookmakerOdds));
        return new ComboEvaluation
        {
            CombinedOdds = combinedOdds,
            SuccessProbability = prob,
            ExpectedValue = prob * combinedOdds - 1,
            RiskLevel = combinedOdds > 10 ? "Très élevé" : combinedOdds > 5 ? "Élevé" : "Modéré",
            AiRecommendation = raw,
            RecommendedStake = 0
        };
    }

    // DTOs pour désérialisation JSON
    private class AiAnalysisDto
    {
        public double HomeWinProbability { get; set; }
        public double DrawProbability { get; set; }
        public double AwayWinProbability { get; set; }
        public double ConfidenceScore { get; set; }
        public string? ForumSentiment { get; set; }
        public string? InjuryImpact { get; set; }
        public string? AnalysisSummary { get; set; }
        public List<SuggestionDto>? Suggestions { get; set; }
    }

    private class SuggestionDto
    {
        public string Market { get; set; } = "MATCH_WINNER";
        public string Description { get; set; } = string.Empty;
        public double TrueOdds { get; set; }
        public double BookmakerOdds { get; set; }
        public double ValueEdge { get; set; }
        public double KellyFraction { get; set; }
        public bool IsValueBet { get; set; }
        public string? Bookmaker { get; set; }
    }

    private class ComboEvaluationDto
    {
        public double CombinedOdds { get; set; }
        public double SuccessProbability { get; set; }
        public double ExpectedValue { get; set; }
        public string? RiskLevel { get; set; }
        public string? AiRecommendation { get; set; }
        public double RecommendedStake { get; set; }
    }
}
