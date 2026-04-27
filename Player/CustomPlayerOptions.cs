using Lavalink4NET.Players.Queued;

namespace DiscordBot.Player;

public record CustomPlayerOptions : QueuedLavalinkPlayerOptions {
    public ulong TextChannelId { get; set; }
}