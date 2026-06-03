using ParisSpo.Domain.Models;

namespace ParisSpo.Domain.Interfaces;

public interface IAiAnalysisService
{
    Task<AiAnalysis> AnalyzeMatchAsync(Match match);
    Task<ComboEvaluation> EvaluateComboAsync(List<BetSuggestion> selections);
}

public class ComboEvaluation
{
    public double CombinedOdds { get; set; }
    public double SuccessProbability { get; set; }
    public double ExpectedValue { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // Low / Medium / High
    public string AiRecommendation { get; set; } = string.Empty;
    public double RecommendedStake { get; set; }
}
