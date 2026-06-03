namespace ParisSpo.Infrastructure.Config;

public class FootballDataSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4";
}

public class TheOddsApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.the-odds-api.com/v4";
}
