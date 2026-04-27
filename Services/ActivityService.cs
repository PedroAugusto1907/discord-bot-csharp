using NetCord;
using NetCord.Gateway;

namespace DiscordBot.Services;

public class ActivityService(GatewayClient client) {
    private HashSet<ulong> _startupGuildIds = [];
    private int _guildCount;
    public int GuildCount => _guildCount;

    public Task InitializeAsync(IEnumerable<ulong> guildIds) {
        var ids = guildIds.ToList();
        _startupGuildIds = [..ids];
        Interlocked.Exchange(ref _guildCount, ids.Count);
        return UpdatePresenceAsync(ids.Count);
    }

    public bool IsStartupGuild(ulong guildId) => _startupGuildIds.Remove(guildId);

    public Task GuildJoinedAsync() =>
        UpdatePresenceAsync(Interlocked.Increment(ref _guildCount));

    public Task GuildLeftAsync() =>
        UpdatePresenceAsync(Interlocked.Decrement(ref _guildCount));

    private async Task UpdatePresenceAsync(int count) {
        await client.UpdatePresenceAsync(new PresenceProperties(UserStatusType.Online)
            .WithActivities([
                new UserActivityProperties(
                    $"Ouvindo música em {count} servidor{(count != 1 ? "es" : "")}",
                    UserActivityType.Streaming)
            ]));
    }
}