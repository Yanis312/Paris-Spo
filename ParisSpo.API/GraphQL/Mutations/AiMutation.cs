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
        foreach (var match in matches)
        {
            match.AiAnalysis = await aiService.AnalyzeMatchAsync(match);
            await matchRepo.UpsertAsync(match);
        }
        return matches;
    }
}
