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
        // Normalise les probabilités pour sommer à 1.0
        var total = dto.HomeWinProbability + dto.DrawProbability + dto.AwayWinProbability;
        if (total <= 0) total = 1;

        var bestOdds = match.Odds.FirstOrDefault();

        return new AiAnalysis
        {
            HomeWinProbability = dto.HomeWinProbability / total,
            DrawProbability = dto.DrawProbability / total,
            AwayWinProbability = dto.AwayWinProbability / total,
            ConfidenceScore = Math.Clamp(dto.ConfidenceScore, 0, 100),
            ForumSentiment = dto.ForumSentiment ?? "Sentiment non disponible",
            InjuryImpact = dto.InjuryImpact ?? "Données blessures non disponibles",
            AnalysisSummary = dto.AnalysisSummary ?? string.Empty,
            GeneratedAt = DateTime.UtcNow,
            Suggestions = dto.Suggestions?.Select(s => new BetSuggestion
            {
                Market = Enum.TryParse<MarketType>(s.Market, true, out var m) ? m : MarketType.MatchWinner,
                Description = s.Description,
                TrueOdds = s.TrueOdds,
                BookmakerOdds = s.BookmakerOdds,
                ValueEdge = s.ValueEdge,
                KellyFraction = Math.Clamp(s.KellyFraction, 0, 0.25), // max 25% bankroll
                IsValueBet = s.IsValueBet && s.ValueEdge > 2,          // min 2% edge
                Bookmaker = s.Bookmaker ?? bestOdds?.Bookmaker ?? "unknown"
            }).ToList() ?? []
        };
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
