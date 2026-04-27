using DiscordBot.Player;
using Lavalink4NET;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot.Commands;

public class Disconnect(IAudioService audioService) : ApplicationCommandModule<SlashCommandContext> {
    [SlashCommand("dc", "Desconecta o bot")]
    public async Task DisconnectAsync() {
        await RespondAsync(InteractionCallback.DeferredMessage(flags: MessageFlags.Ephemeral));

        ulong? voiceChannelId;

        try {
            var voiceState = await Context.Guild.GetUserVoiceStateAsync(Context.User.Id);
            voiceChannelId = voiceState?.ChannelId;
        } catch {
            await FollowupAsync("Você não está em um canal de voz");
            return;
        }

        if (Context.Guild is not null && !audioService.Players.HasPlayer(Context.Guild.Id)) {
            await FollowupAsync("Bot não está conectado");
            return;
        }

        var player = await audioService.Players.GetPlayerAsync<CustomPlayer>(Context.Guild.Id);

        if (player is null) {
            await FollowupAsync("Não foi possível obter o player");
            return;
        }

        if (player.VoiceChannelId != voiceChannelId) {
            await FollowupAsync("Você precisa estar no mesmo canal que o bot");
            return;
        }

        await player.DisposeAsync();
        await FollowupAsync("Desconectado com sucesso");
    }
}