using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ParisSpo.Domain.Enums;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Config;

namespace ParisSpo.Infrastructure.ExternalApis;

public class FootballDataService : IFootballDataService
{
    private readonly HttpClient _http;

    // football-data.org competition IDs
    private static readonly Dictionary<int, Competition> CompetitionMap = new()
    {
        { 2021, Competition.PremierLeague },
        { 2015, Competition.Ligue1 },
        { 2014, Competition.LaLiga },
        { 2019, Competition.SerieA },
        { 2002, Competition.Bundesliga },
        { 2001, Competition.ChampionsLeague },
        { 2018, Competition.EuroCup },
        { 2000, Competition.WorldCup }
    };

    public FootballDataService(IOptions<FootballDataSettings> settings)
    {
        _http = new HttpClient { BaseAddress = new Uri(settings.Value.BaseUrl) };
        _http.DefaultRequestHeaders.Add("X-Auth-Token", settings.Value.ApiKey);
    }

    public async Task<List<Match>> GetTodayMatchesAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await _http.GetFromJsonAsync<MatchesResponse>($"/v4/matches?dateFrom={today}&dateTo={today}");
        return response?.Matches.Select(MapMatch).ToList() ?? [];
    }

    public async Task<List<Match>> GetMatchesByDateAsync(DateTime date)
    {
        var d = date.ToString("yyyy-MM-dd");
        var response = await _http.GetFromJsonAsync<MatchesResponse>($"/v4/matches?dateFrom={d}&dateTo={d}");
        return response?.Matches.Select(MapMatch).ToList() ?? [];
    }

    public async Task<List<Match>> GetMatchesByCompetitionAsync(Competition competition, DateTime dateFrom, DateTime dateTo)
    {
        var compId = CompetitionMap.FirstOrDefault(x => x.Value == competition).Key;
        if (compId == 0) return [];

        var from = dateFrom.ToString("yyyy-MM-dd");
        var to = dateTo.ToString("yyyy-MM-dd");
        var response = await _http.GetFromJsonAsync<MatchesResponse>(
            $"/v4/competitions/{compId}/matches?dateFrom={from}&dateTo={to}&status=SCHEDULED");
        return response?.Matches.Select(MapMatch).ToList() ?? [];
    }

    public async Task<Team?> GetTeamAsync(int teamId)
    {
        var response = await _http.GetFromJsonAsync<FdTeam>($"/v4/teams/{teamId}");
        return response == null ? null : MapTeam(response);
    }

    public async Task<List<PlayerInjury>> GetInjuriesAsync(int teamId)
    {
        // football-data.org free tier doesn't expose injuries directly
        // returns squad — we mark injured players from squad status
        var response = await _http.GetFromJsonAsync<FdTeamSquad>($"/v4/teams/{teamId}");
        return response?.Squad
            .Where(p => p.Status == "INJURED" || p.Status == "SUSPENDED")
            .Select(p => new PlayerInjury
            {
                PlayerName = p.Name,
                Position = p.Position ?? "Unknown",
                InjuryType = p.Status == "SUSPENDED" ? "Suspension" : "Blessure",
                Reason = p.Status ?? string.Empty
            })
            .ToList() ?? [];
    }

    private static Match MapMatch(FdMatch m)
    {
        var competition = CompetitionMap.GetValueOrDefault(m.Competition?.Id ?? 0, Competition.ChampionsLeague);
        return new Match
        {
            ApiFootballId = m.Id,
            HomeTeamName = m.HomeTeam?.ShortName ?? m.HomeTeam?.Name ?? "?",
            AwayTeamName = m.AwayTeam?.ShortName ?? m.AwayTeam?.Name ?? "?",
            Competition = competition,
            CompetitionName = m.Competition?.Name ?? string.Empty,
            KickOff = m.UtcDate,
            Status = m.Status switch
            {
                "SCHEDULED" or "TIMED" => MatchStatus.Scheduled,
                "IN_PLAY" or "PAUSED" => MatchStatus.Live,
                "FINISHED" => MatchStatus.Finished,
                "POSTPONED" => MatchStatus.Postponed,
                "CANCELLED" or "SUSPENDED" => MatchStatus.Cancelled,
                _ => MatchStatus.Scheduled
            },
            Score = m.Score?.FullTime == null ? null : new MatchScore
            {
                HomeGoals = m.Score.FullTime.Home ?? 0,
                AwayGoals = m.Score.FullTime.Away ?? 0,
                HomeGoalsHT = m.Score.HalfTime?.Home,
                AwayGoalsHT = m.Score.HalfTime?.Away
            }
        };
    }

    private static Team MapTeam(FdTeam t) => new()
    {
        ApiFootballId = t.Id,
        Name = t.Name,
        ShortName = t.ShortName ?? t.Tla ?? t.Name,
        Country = t.Area?.Name ?? string.Empty,
        LogoUrl = t.Crest ?? string.Empty
    };

    // DTOs
    private record MatchesResponse([property: JsonPropertyName("matches")] List<FdMatch> Matches);

    private record FdMatch(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("utcDate")] DateTime UtcDate,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("competition")] FdCompetition? Competition,
        [property: JsonPropertyName("homeTeam")] FdTeamRef? HomeTeam,
        [property: JsonPropertyName("awayTeam")] FdTeamRef? AwayTeam,
        [property: JsonPropertyName("score")] FdScore? Score);

    private record FdCompetition(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name);

    private record FdTeamRef(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("shortName")] string? ShortName);

    private record FdScore(
        [property: JsonPropertyName("fullTime")] FdScoreDetail? FullTime,
        [property: JsonPropertyName("halfTime")] FdScoreDetail? HalfTime);

    private record FdScoreDetail(
        [property: JsonPropertyName("home")] int? Home,
        [property: JsonPropertyName("away")] int? Away);

    private record FdTeam(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("shortName")] string? ShortName,
        [property: JsonPropertyName("tla")] string? Tla,
        [property: JsonPropertyName("crest")] string? Crest,
        [property: JsonPropertyName("area")] FdArea? Area);

    private record FdTeamSquad(
        [property: JsonPropertyName("squad")] List<FdPlayer> Squad);

    private record FdPlayer(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("position")] string? Position,
        [property: JsonPropertyName("status")] string? Status);

    private record FdArea(
        [property: JsonPropertyName("name")] string Name);
}
