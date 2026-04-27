using DiscordBot.Player;
using Lavalink4NET;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace DiscordBot.Events;

public class VoiceStateUpdate(IAudioService audioService, GatewayClient client) : IVoiceStateUpdateGatewayHandler {
    public async ValueTask HandleAsync(VoiceState args) {
        audioService.Players.TryGetPlayer(args.GuildId, out CustomPlayer? player);
        if (player is null) return;

        if (args.UserId == client.Cache.User?.Id) return;

        if (args.ChannelId == player.VoiceChannelId) return;

        var guild = client.Cache.Guilds[args.GuildId];

        var botId = client.Cache.User?.Id;

        var users = guild
            .VoiceStates.Values.Where(v => v.ChannelId == player.VoiceChannelId)
            .Count(v => v.UserId != botId && v.UserId != args.UserId);

        if (users == 0) {
            await player.DisposeAsync();
        }
    }
}