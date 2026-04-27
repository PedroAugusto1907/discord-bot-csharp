using DiscordBot.Services;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace DiscordBot.Events;

public class GuildJoin(ActivityService activityService, ILogger<GuildJoin> logger) : IGuildCreateGatewayHandler {
    public async ValueTask HandleAsync(GuildCreateEventArgs args) {
        logger.LogInformation("Bot online no servidor: {GuildName} (ID: {GuildId}) — Membros: {UserCount}",
            args.Guild.Name,
            args.Guild.Id,
            args.Guild.UserCount);

        if (!activityService.IsStartupGuild(args.GuildId)) {
            await activityService.GuildJoinedAsync();
        }
    }
}