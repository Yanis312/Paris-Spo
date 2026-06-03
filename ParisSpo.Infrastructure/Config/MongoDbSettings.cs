namespace ParisSpo.Infrastructure.Config;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "ParisSpo";
}
