namespace DiscordBot.Config;

public class LavalinkConfig {
    public required string Host { get; set; }
    public required string Identificador { get; set; }
    public required string Senha { get; set; }
    public required int TimeoutSeconds { get; set; }
}