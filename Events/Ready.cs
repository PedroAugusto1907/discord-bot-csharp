using DiscordBot.Repository;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace DiscordBot.Events;

public class Ready(ActivityService activityService, GifRepository gifRepository,RestClient restClient, ILogger<Ready> logger, IConfiguration config) : IReadyGatewayHandler {
    public async ValueTask HandleAsync(ReadyEventArgs args) {
        logger.LogInformation("Bot está rodando! Logado como {Username}#{Discriminator} | Servidores: {GuildCount}",
            args.User.Username,
            args.User.Discriminator,
            args.GuildIds.Count);

        await activityService.InitializeAsync(args.GuildIds);

        await gifRepository.Load();
        
        await UpdateAvatarAsync();
    }

    private async Task UpdateAvatarAsync() {
        var fileName = config["Bot:AvatarFileName"];
        
        if (string.IsNullOrWhiteSpace(fileName)) {
            logger.LogWarning("Bot:AvatarFileName não configurado");
            return;
        }
        
        var avatarPath = Path.Combine(AppContext.BaseDirectory, fileName);

        if (!File.Exists(avatarPath)) {
            logger.LogWarning("Avatar não encontrado em {Path}", avatarPath);
            return;
        }

        try {
            var bytes = await File.ReadAllBytesAsync(avatarPath);
            await restClient.ModifyCurrentUserAsync(opt => opt.WithAvatar(new ImageProperties(ImageFormat.Gif, bytes)));

            logger.LogInformation("Avatar atualizado com sucesso");
        } catch (Exception ex) {
            logger.LogWarning("Não foi possível atualizar o avatar: {Erro}", ex.Message);
        }
    }
}