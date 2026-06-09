using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;

namespace ParisSpo.API.GraphQL.Mutations;

[MutationType]
public class AiMutation
{
    public async Task<Match> AnalyzeMatchAsync(
        string matchId,
        [Service] IMatchRepository matchRepo,
        [Service] IAiAnalysisService aiService)
    {
        var match = await matchRepo.GetByIdAsync(matchId)
            ?? throw new GraphQLException($"Match {matchId} not found");

        match.AiAnalysis = await aiService.AnalyzeMatchAsync(match);
        await matchRepo.UpsertAsync(match);
        return match;
    }

    public async Task<List<Match>> AnalyzeTodayMatchesAsync(
        [Service] IMatchRepository matchRepo,
        [Service] IAiAnalysisService aiService)
    {
        var matches = await matchRepo.GetTodayMatchesAsync();

        // Analyse en parallèle par batch de 4 (évite rate limit OpenRouter)
        foreach (var batch in matches.Chunk(4))
        {
            await Task.WhenAll(batch.Select(async match =>
            {
                match.AiAnalysis = await aiService.AnalyzeMatchAsync(match);
                await matchRepo.UpsertAsync(match);
            }));
        }
        return matches;
    }

    public async Task<List<Match>> AnalyzeMatchesByDateAsync(
        DateTime date,
        [Service] IMatchRepository matchRepo,
        [Service] IAiAnalysisService aiService)
    {
        var matches = await matchRepo.GetMatchesByDateAsync(date);
        foreach (var batch in matches.Chunk(4))
        {
            await Task.WhenAll(batch.Select(async match =>
            {
                match.AiAnalysis = await aiService.AnalyzeMatchAsync(match);
                await matchRepo.UpsertAsync(match);
            }));
        }
        return matches;
    }
}
