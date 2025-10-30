namespace Tolltech.Runner;

public class AppSettings
{
    public required string ConnectionString { get; set; }
    public required BotSettings[] BotSettings { get; set; } = [];
}
        
public class BotSettings
{
    public required string Token { get; set; }
    public required string BotName { get; set; }
    public required string CustomSettings { get; set; }
}