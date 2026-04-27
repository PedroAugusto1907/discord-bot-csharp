using DiscordBot.Services;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace DiscordBot.Events;

public class GuildLeave(ActivityService activityService, ILogger<GuildLeave> logger) : IGuildDeleteGatewayHandler {
    public async ValueTask HandleAsync(GuildDeleteEventArgs args) {
        if (args.IsUnavailable) return;

        logger.LogInformation("Bot removido do servidor ID: {GuildId}", args.GuildId);

        await activityService.GuildLeftAsync();
    }
}