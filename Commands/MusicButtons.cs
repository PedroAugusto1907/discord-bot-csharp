using DiscordBot.Player;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using NetCord.Services.ComponentInteractions;
using Lavalink4NET.Players;
using NetCord;
using NetCord.Rest;

namespace DiscordBot.Commands;

public class MusicButtons(IAudioService audioService) : ComponentInteractionModule<ButtonInteractionContext> {
    private async Task<CustomPlayer?> GetPlayerAsync() {
        var player = await audioService.Players.GetPlayerAsync<CustomPlayer>(Context.Guild.Id);

        if (player is null) {
            await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties()
                .WithContent("Não há nada tocando")
                .WithFlags(MessageFlags.Ephemeral)));

            return null;
        }

        if (player.CurrentMessageId != Context.Message.Id) {
            await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties()
                .WithContent("Estes controles estão desatualizados")
                .WithFlags(MessageFlags.Ephemeral)));

            return null;
        }

        try {
            var voiceState = await Context.Guild.GetUserVoiceStateAsync(Context.User.Id);

            if (voiceState?.ChannelId is null || voiceState.ChannelId != player.VoiceChannelId) {
                await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties()
                    .WithContent("Você precisa estar no mesmo canal de voz que o bot")
                    .WithFlags(MessageFlags.Ephemeral)));

                return null;
            }
        } catch (Exception) {
            await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties()
                .WithContent("Você precisa estar no mesmo canal de voz que o bot")
                .WithFlags(MessageFlags.Ephemeral)));
            
            return null;
        }
        
        return player;
    }

    [ComponentInteraction("bt-skip")]
    public async Task SkipAsync() {
        var player = await GetPlayerAsync();
        if (player is null) return;

        await player.Lock.WaitAsync();

        try {
            player.IsManualSkip = true;
            await player.SkipAsync();
            await RespondAsync(InteractionCallback.DeferredModifyMessage);
        } finally {
            player.Lock.Release();
        }
    }

    [ComponentInteraction("bt-pausePlay")]
    public async Task PausePlayAsync() {
        var player = await GetPlayerAsync();
        if (player is null) return;

        await player.Lock.WaitAsync();

        try {
            if (player.State == PlayerState.Paused)
                await player.ResumeAsync();
            else
                await player.PauseAsync();

            await player.UpdateControlsAsync();

            await RespondAsync(InteractionCallback.DeferredModifyMessage);
        } finally {
            player.Lock.Release();
        }
    }

    [ComponentInteraction("bt-shuffle")]
    public async Task ShuffleAsync() {
        var player = await GetPlayerAsync();
        if (player is null) return;

        await player.Lock.WaitAsync();

        try {
            await player.Queue.ShuffleAsync();
            await RespondAsync(InteractionCallback.DeferredModifyMessage);
        } finally {
            player.Lock.Release();
        }
    }

    [ComponentInteraction("bt-loop")]
    public async Task LoopAsync() {
        var player = await GetPlayerAsync();
        if (player is null) return;

        await player.Lock.WaitAsync();

        try {
            player.RepeatMode = player.RepeatMode == TrackRepeatMode.Track ? TrackRepeatMode.None : TrackRepeatMode.Track;

            await player.UpdateControlsAsync();

            await RespondAsync(InteractionCallback.DeferredModifyMessage);
        } finally {
            player.Lock.Release();
        }
    }

    [ComponentInteraction("bt-stop")]
    public async Task StopAsync() {
        var player = await GetPlayerAsync();
        if (player is null) return;

        await player.Lock.WaitAsync();

        try {
            await player.StopAsync();
            await RespondAsync(InteractionCallback.DeferredModifyMessage);
        } finally {
            player.Lock.Release();
        }
    }
}