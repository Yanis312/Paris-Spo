using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace ParisSpo.AI.Services;

/// <summary>
/// Builds a Semantic Kernel instance pointed at OpenRouter with automatic fallback
/// through the free model list when a model hits its daily limit (HTTP 429).
/// </summary>
public class OpenRouterKernelFactory
{
    // Ordered by quality — best first, smallest/weakest last
    public static readonly string[] FreeModels =
    [
        "moonshotai/kimi-k2:free",
        "qwen/qwen3-coder-480b-a35b:free",
        "qwen/qwen3-235b-a22b:free",
        "openai/gpt-oss-120b:free",
        "meta-llama/llama-3.3-70b-instruct:free",
        "google/gemma-2-27b-it:free",
        "nvidia/llama-3.1-nemotron-70b-instruct:free",
        "google/gemma-4-31b:free",
        "google/gemma-4-26b-a4b:free",
        "z-ai/glm-4.5-air:free",
        "nousresearch/hermes-3-405b-instruct:free",
        "meta-llama/llama-3.2-3b-instruct:free",
        "liquidai/lfm2.5-1.2b-instruct:free",
    ];

    private readonly string _apiKey;
    private readonly ILoggerFactory? _loggerFactory;

    public OpenRouterKernelFactory(string apiKey, ILoggerFactory? loggerFactory = null)
    {
        _apiKey = apiKey;
        _loggerFactory = loggerFactory;
    }

    public Kernel BuildForModel(string modelId)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: _apiKey,
            httpClient: BuildHttpClient());

        if (_loggerFactory != null)
            builder.Services.AddSingleton(_loggerFactory);

        return builder.Build();
    }

    private HttpClient BuildHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/")
        };
        client.DefaultRequestHeaders.Add("HTTP-Referer", "https://paris-spo.app");
        client.DefaultRequestHeaders.Add("X-Title", "Paris-Spo");
        return client;
    }
}
