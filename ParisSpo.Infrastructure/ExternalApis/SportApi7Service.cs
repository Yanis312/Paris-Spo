using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.ExternalApis;

public class RateLimitException(string message) : Exception(message);

/// <summary>
/// SportAPI7 (Sofascore) — real fixtures + odds for football.
/// Single source: scheduled events by date, event odds (1X2 + markets).
/// </summary>
public class SportApi7Service : IFootballDataService, IOddsService
{
    private readonly HttpClient _http;

    public SportApi7Service(IOptions<SportApi7Settings> settings)
    {
        _http = new HttpClient { BaseAddress = new Uri(settings.Value.BaseUrl) };
        _http.DefaultRequestHeaders.Add("x-rapidapi-host", settings.Value.Host);
        _http.DefaultRequestHeaders.Add("x-rapidapi-key", settings.Value.ApiKey);
    }

    // ── IFootballDataService ──

    public Task<List<Match>> GetTodayMatchesAsync()
        => GetMatchesByDateAsync(DateTime.UtcNow);

    public async Task<List<Match>> GetMatchesByDateAsync(DateTime date)
    {
        var d = date.ToString("yyyy-MM-dd");
        var httpResp = await _http.GetAsync($"/api/v1/sport/football/scheduled-events/{d}");

        if (httpResp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            throw new RateLimitException("SportAPI7 quota épuisé (429). Réessaie demain ou upgrade le plan RapidAPI.");

        httpResp.EnsureSuccessStatusCode();
        var resp = await httpResp.Content.ReadFromJsonAsync<ScheduledEventsResponse>();

        if (resp?.Events == null) return [];

        return resp.Events
            .Where(e => e.Tournament != null && IsRelevant(e))
            .Select(MapMatch)
            .ToList();
    }

    public Task<List<Match>> GetMatchesByCompetitionAsync(Competition competition, DateTime from, DateTime to)
        => GetMatchesByDateAsync(from); // simplifié — SportAPI7 filtre par date

    public Task<Team?> GetTeamAsync(int teamId) => Task.FromResult<Team?>(null);

    public Task<List<PlayerInjury>> GetInjuriesAsync(int teamId) => Task.FromResult(new List<PlayerInjury>());

    // ── IOddsService ──

    public async Task<List<MatchOdds>> GetOddsForMatchAsync(string homeTeam, string awayTeam, DateTime kickOff)
    {
        // SportAPI7 odds need eventId — handled in sync via GetOddsByEventIdAsync
        return [];
    }

    public Task<List<OddsEvent>> GetUpcomingOddsAsync() => Task.FromResult(new List<OddsEvent>());

    /// <summary>Fetch 1X2 odds for a specific SportAPI7 event.</summary>
    public async Task<List<MatchOdds>> GetOddsByEventIdAsync(int eventId)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<OddsResponse>($"/api/v1/event/{eventId}/odds/1/all");
            var market = resp?.Markets?.FirstOrDefault(m => m.MarketGroup == "1X2" || m.MarketId == 1);
            if (market?.Choices == null) return [];

            double Get(string name) => market.Choices
                .Where(c => c.Name == name)
                .Select(c => FractionToDecimal(c.FractionalValue))
                .FirstOrDefault();

            var home = Get("1");
            var draw = Get("X");
            var away = Get("2");

            if (home == 0 && draw == 0 && away == 0) return [];

            return [new MatchOdds
            {
                Bookmaker = "Sofascore",
                HomeWin = home,
                Draw = draw,
                AwayWin = away,
                FetchedAt = DateTime.UtcNow
            }];
        }
        catch
        {
            return [];
        }
    }

    // ── Mapping ──

    // Tournois gardés — gros championnats + compétitions internationales
    private static readonly string[] RelevantKeywords =
    [
        "premier league", "laliga", "la liga", "serie a", "bundesliga", "ligue 1",
        "champions league", "europa league", "conference league",
        "world cup", "euro", "nations league", "friendly", "amical",
        "copa", "libertadores", "eredivisie", "primeira liga", "championship",
    ];

    private static bool IsRelevant(Sa7Event e)
    {
        var t = e.Tournament!.Name?.ToLowerInvariant() ?? "";

        // exclure jeunes + féminin + réserves
        if (t.Contains("u17") || t.Contains("u19") || t.Contains("u20") || t.Contains("u21") ||
            t.Contains("u23") || t.Contains("women") || t.Contains(" ii") || t.Contains("next pro") ||
            t.Contains("reserve") || t.Contains("youth"))
            return false;

        // garder uniquement les tournois pertinents
        return RelevantKeywords.Any(k => t.Contains(k));
    }

    private static Match MapMatch(Sa7Event e)
    {
        var comp = MapCompetition(e.Tournament!.Name ?? "");
        return new Match
        {
            ApiFootballId = e.Id,
            HomeTeamName = e.HomeTeam?.Name ?? "?",
            AwayTeamName = e.AwayTeam?.Name ?? "?",
            CompetitionName = e.Tournament.Name ?? "Football",
            Competition = comp,
            KickOff = DateTimeOffset.FromUnixTimeSeconds(e.StartTimestamp).UtcDateTime,
            Status = MapStatus(e.Status?.Type),
            Score = e.HomeScore?.Current != null ? new MatchScore
            {
                HomeGoals = e.HomeScore.Current ?? 0,
                AwayGoals = e.AwayScore?.Current ?? 0
            } : null
        };
    }

    private static Competition MapCompetition(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("world cup")) return Competition.WorldCup;
        if (n.Contains("premier league")) return Competition.PremierLeague;
        if (n.Contains("laliga") || n.Contains("la liga")) return Competition.LaLiga;
        if (n.Contains("serie a")) return Competition.SerieA;
        if (n.Contains("bundesliga")) return Competition.Bundesliga;
        if (n.Contains("ligue 1")) return Competition.Ligue1;
        if (n.Contains("champions")) return Competition.ChampionsLeague;
        if (n.Contains("europa")) return Competition.EuropaLeague;
        return Competition.WorldCup; // default fallback (friendlies/internationals)
    }

    private static MatchStatus MapStatus(string? type) => type switch
    {
        "notstarted" => MatchStatus.Scheduled,
        "inprogress" => MatchStatus.Live,
        "finished" => MatchStatus.Finished,
        "postponed" => MatchStatus.Postponed,
        "canceled" => MatchStatus.Cancelled,
        _ => MatchStatus.Scheduled
    };

    private static double FractionToDecimal(string? fractional)
    {
        if (string.IsNullOrEmpty(fractional)) return 0;
        var parts = fractional.Split('/');
        if (parts.Length != 2) return 0;
        if (double.TryParse(parts[0], out var num) && double.TryParse(parts[1], out var den) && den != 0)
            return Math.Round(num / den + 1, 2); // fractional → decimal odds
        return 0;
    }

    // ── DTOs ──

    private record ScheduledEventsResponse(
        [property: JsonPropertyName("events")] List<Sa7Event>? Events);

    private record Sa7Event(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("startTimestamp")] long StartTimestamp,
        [property: JsonPropertyName("tournament")] Sa7Tournament? Tournament,
        [property: JsonPropertyName("homeTeam")] Sa7Team? HomeTeam,
        [property: JsonPropertyName("awayTeam")] Sa7Team? AwayTeam,
        [property: JsonPropertyName("homeScore")] Sa7Score? HomeScore,
        [property: JsonPropertyName("awayScore")] Sa7Score? AwayScore,
        [property: JsonPropertyName("status")] Sa7Status? Status);

    private record Sa7Tournament(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("category")] Sa7Category? Category);

    private record Sa7Category([property: JsonPropertyName("name")] string? Name);

    private record Sa7Team([property: JsonPropertyName("name")] string? Name);

    private record Sa7Score([property: JsonPropertyName("current")] int? Current);

    private record Sa7Status([property: JsonPropertyName("type")] string? Type);

    private record OddsResponse(
        [property: JsonPropertyName("markets")] List<OddsMarket>? Markets);

    private record OddsMarket(
        [property: JsonPropertyName("marketId")] int MarketId,
        [property: JsonPropertyName("marketGroup")] string? MarketGroup,
        [property: JsonPropertyName("choices")] List<OddsChoice>? Choices);

    private record OddsChoice(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("fractionalValue")] string? FractionalValue);
}
