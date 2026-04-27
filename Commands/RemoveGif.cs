using DiscordBot.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot.Commands;

public class RemoveGif(GifRepository gifRepository, IConfiguration config, ILogger<RemoveGif> logger)
    : ApplicationCommandModule<MessageCommandContext> {

    [MessageCommand("Remover GIF")]
    public async Task RemoveGifAsync() {
        // Restrição por usuário
        var ownerId = config["Bot:OwnerId"];
        if (ownerId is null || Context.User.Id.ToString() != ownerId) {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties()
                    .WithContent("❌ Você não tem permissão para usar este comando.")
                    .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        var message = Context.Target;
        
        if (message.Author.Id != Context.Client.Id) {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties()
                    .WithContent("❌ Este comando só pode ser usado em mensagens enviadas pelo bot.")
                    .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        var gifUrl = message.Embeds.FirstOrDefault()?.Url
                  ?? message.Embeds.FirstOrDefault()?.Image?.Url
                  ?? message.Attachments.FirstOrDefault()?.Url
                  ?? message.Content;

        if (string.IsNullOrWhiteSpace(gifUrl)) {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties()
                    .WithContent("❌ Nenhum GIF encontrado nessa mensagem.")
                    .WithFlags(MessageFlags.Ephemeral)));
            return;
        }

        var removed = await gifRepository.RemoveAsync(gifUrl);

        if (removed) {
            logger.LogInformation("GIF removido por {User}: {Url}", Context.User, gifUrl);

            // Apaga a mensagem do GIF inválido
            await message.DeleteAsync();

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties()
                    .WithContent($"✅ GIF removido do banco e mensagem apagada.\n`{gifUrl}`")
                    .WithFlags(MessageFlags.Ephemeral)));
        } else {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties()
                    .WithContent("⚠️ Esse GIF não está no banco.")
                    .WithFlags(MessageFlags.Ephemeral)));
        }
    }
}