using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.ExternalApis;

public class TheOddsApiService : IOddsService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    // Sports keys for football-data.org competitions
    private static readonly string[] FootballSports =
    [
        "soccer_england_league1",
        "soccer_france_ligue_1",
        "soccer_spain_la_liga",
        "soccer_italy_serie_a",
        "soccer_germany_bundesliga",
        "soccer_uefa_champs_league",
        "soccer_uefa_europa_league"
    ];

    private static readonly string[] SupportedBookmakers =
        ["bet365", "unibet", "winamax", "pinnacle", "betfair"];

    public TheOddsApiService(IOptions<TheOddsApiSettings> settings)
    {
        _apiKey = settings.Value.ApiKey;
        _http = new HttpClient { BaseAddress = new Uri(settings.Value.BaseUrl) };
    }

    /// <summary>Récupère toutes les cotes WC 2026 (1 appel API). Retourne map par "home|away".</summary>
    public async Task<Dictionary<string, List<MatchOdds>>> GetWorldCupOddsMapAsync()
    {
        var result = new Dictionary<string, List<MatchOdds>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var url = $"/v4/sports/soccer_fifa_world_cup/odds/?apiKey={_apiKey}&regions=eu&markets=h2h,totals";
            var events = await _http.GetFromJsonAsync<List<OddsEvent>>(url) ?? [];
            foreach (var e in events)
            {
                var odds = MapOdds(e);
                if (odds.Count > 0)
                    result[NormKey(e.HomeTeam, e.AwayTeam)] = odds;
            }
        }
        catch { /* quota or network — return what we have */ }
        return result;
    }

    private static string NormKey(string home, string away)
        => $"{Simplify(home)}|{Simplify(away)}";

    private static string Simplify(string s)
        => new string(s.ToLowerInvariant().Where(char.IsLetter).ToArray());

    public async Task<List<MatchOdds>> GetOddsForMatchAsync(string homeTeam, string awayTeam, DateTime kickOff)
    {
        var allOdds = new List<MatchOdds>();

        foreach (var sport in FootballSports)
        {
            var events = await FetchOddsAsync(sport);
            var match = events.FirstOrDefault(e =>
                IsTeamMatch(e.HomeTeam, homeTeam) && IsTeamMatch(e.AwayTeam, awayTeam));

            if (match == null) continue;

            allOdds.AddRange(MapOdds(match));
            break;
        }

        return allOdds;
    }

    public async Task<List<Domain.Interfaces.OddsEvent>> GetUpcomingOddsAsync()
    {
        var allEvents = new List<Domain.Interfaces.OddsEvent>();
        foreach (var sport in FootballSports)
        {
            var events = await FetchOddsAsync(sport);
            allEvents.AddRange(events.Select(e => new Domain.Interfaces.OddsEvent
            {
                Id = e.Id,
                SportKey = e.SportKey,
                CommenceTime = e.CommenceTime,
                HomeTeam = e.HomeTeam,
                AwayTeam = e.AwayTeam,
                Odds = MapOdds(e)
            }));
        }
        return allEvents;
    }

    private async Task<List<OddsEvent>> FetchOddsAsync(string sport)
    {
        try
        {
            var url = $"/v4/sports/{sport}/odds?apiKey={_apiKey}&regions=eu&markets=h2h,totals&bookmakers={string.Join(",", SupportedBookmakers)}";
            return await _http.GetFromJsonAsync<List<OddsEvent>>(url) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<MatchOdds> MapOdds(OddsEvent e)
    {
        var result = new List<MatchOdds>();
        foreach (var bookmaker in e.Bookmakers)
        {
            var odds = new MatchOdds { Bookmaker = bookmaker.Key };

            var h2h = bookmaker.Markets.FirstOrDefault(m => m.Key == "h2h");
            if (h2h != null)
            {
                odds.HomeWin = h2h.Outcomes.FirstOrDefault(o => o.Name == e.HomeTeam)?.Price ?? 0;
                odds.AwayWin = h2h.Outcomes.FirstOrDefault(o => o.Name == e.AwayTeam)?.Price ?? 0;
                odds.Draw = h2h.Outcomes.FirstOrDefault(o => o.Name == "Draw")?.Price ?? 0;
            }

            var totals = bookmaker.Markets.FirstOrDefault(m => m.Key == "totals");
            if (totals != null)
            {
                odds.Over25 = totals.Outcomes.FirstOrDefault(o => o.Name == "Over" && o.Point == 2.5)?.Price;
                odds.Under25 = totals.Outcomes.FirstOrDefault(o => o.Name == "Under" && o.Point == 2.5)?.Price;
            }

            result.Add(odds);
        }
        return result;
    }

    private static bool IsTeamMatch(string apiName, string localName)
    {
        apiName = apiName.ToLowerInvariant();
        localName = localName.ToLowerInvariant();
        return apiName.Contains(localName) || localName.Contains(apiName) ||
               apiName.Split(' ').Intersect(localName.Split(' ')).Any();
    }

    // DTOs
    public record OddsEvent(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("sport_key")] string SportKey,
        [property: JsonPropertyName("commence_time")] DateTime CommenceTime,
        [property: JsonPropertyName("home_team")] string HomeTeam,
        [property: JsonPropertyName("away_team")] string AwayTeam,
        [property: JsonPropertyName("bookmakers")] List<OddsBookmaker> Bookmakers);

    public record OddsBookmaker(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("markets")] List<OddsMarket> Markets);

    public record OddsMarket(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("outcomes")] List<OddsOutcome> Outcomes);

    public record OddsOutcome(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("price")] double Price,
        [property: JsonPropertyName("point")] double? Point);
}
