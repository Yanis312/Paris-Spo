using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace ParisSpo.AI.Services;

/// <summary>
/// Executes a Semantic Kernel prompt against OpenRouter free models.
/// On HTTP 429 (rate limit / daily quota) or any model error,
/// automatically falls back to the next model in the list — infinitely cycling.
/// </summary>
public class FallbackKernelExecutor
{
    private readonly OpenRouterKernelFactory _factory;
    private readonly ILogger<FallbackKernelExecutor> _logger;

    // Tracks which models hit their daily limit — reset at midnight UTC
    private static readonly HashSet<string> _exhaustedModels = [];
    private static DateTime _exhaustedResetTime = DateTime.UtcNow.Date.AddDays(1);

    public FallbackKernelExecutor(OpenRouterKernelFactory factory, ILogger<FallbackKernelExecutor> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string prompt, CancellationToken ct = default)
    {
        ResetExhaustedIfNewDay();

        // Build candidate list: non-exhausted models first, exhausted ones as last resort
        var candidates = OpenRouterKernelFactory.FreeModels
            .Where(m => !_exhaustedModels.Contains(m))
            .Concat(OpenRouterKernelFactory.FreeModels.Where(m => _exhaustedModels.Contains(m)))
            .ToList();

        Exception? lastException = null;

        foreach (var model in candidates)
        {
            try
            {
                _logger.LogDebug("Trying model {Model}", model);
                var kernel = _factory.BuildForModel(model);
                var result = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);
                var text = result.ToString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogInformation("Success with model {Model}", model);
                    return text;
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Model {Model} hit rate limit (429) — marking exhausted", model);
                _exhaustedModels.Add(model);
                lastException = ex;
            }
            catch (Exception ex) when (IsModelUnavailable(ex))
            {
                _logger.LogWarning("Model {Model} unavailable: {Msg} — trying next", model, ex.Message);
                lastException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Model {Model} error: {Msg} — trying next", model, ex.Message);
                lastException = ex;
            }
        }

        // All models failed — return graceful degradation message
        _logger.LogError("All OpenRouter free models failed. Last error: {Error}", lastException?.Message);
        return "Analyse IA temporairement indisponible — tous les modèles gratuits ont atteint leur limite. Réessaie dans quelques heures.";
    }

    private static bool IsModelUnavailable(Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("model") && (
            msg.Contains("unavailable") ||
            msg.Contains("not found") ||
            msg.Contains("overloaded") ||
            msg.Contains("capacity"));
    }

    private static void ResetExhaustedIfNewDay()
    {
        if (DateTime.UtcNow >= _exhaustedResetTime)
        {
            _exhaustedModels.Clear();
            _exhaustedResetTime = DateTime.UtcNow.Date.AddDays(1);
        }
    }
}
