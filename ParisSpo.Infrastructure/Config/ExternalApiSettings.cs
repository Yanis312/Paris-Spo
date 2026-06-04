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

public class SportApi7Settings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Host { get; set; } = "sportapi7.p.rapidapi.com";
    public string BaseUrl { get; set; } = "https://sportapi7.p.rapidapi.com";
}
