using DiscordBot.Player;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot.Commands;

public class Play(IAudioService audioService, RestClient restClient, ILogger<Play> logger) : ApplicationCommandModule<SlashCommandContext> {
    [SlashCommand("play", "Toca uma música ou playlist do youtube!")]
    public async Task PlayAsync([SlashCommandParameter(Description = "Nome | Link")] string query) {
        await RespondAsync(InteractionCallback.DeferredMessage(flags: MessageFlags.Ephemeral));

        ulong? voiceChannelId;

        try {
            var voiceState = await Context.Guild.GetUserVoiceStateAsync(Context.User.Id);
            voiceChannelId = voiceState?.ChannelId;
        } catch {
            await FollowupAsync("Você não está em um canal de voz");
            return;
        }

        try {
            var resultTracks = await audioService.Tracks.LoadTracksAsync(query, TrackSearchMode.YouTube);

            if (!resultTracks.HasMatches) {
                await FollowupAsync("Nenhuma música encontrada");
                return;
            }

            var validTracks = resultTracks
                .Tracks.Where(t => !t.IsLiveStream)
                .ToList();

            if (validTracks.Count == 0) {
                await FollowupAsync("Nenhuma música válida encontrada. OBS: Livestreams não são suportadas!");
                return;
            }

            var resultPlayer = await audioService.Players.RetrieveAsync(Context.Guild.Id,
                voiceChannelId,
                playerFactory: PlayerFactory.Create<CustomPlayer, CustomPlayerOptions>(properties =>
                    new CustomPlayer(properties, restClient, properties.ServiceProvider!.GetRequiredService<ILogger<CustomPlayer>>())),
                options: Options.Create(new CustomPlayerOptions { TextChannelId = Context.Channel.Id }),
                retrieveOptions: new PlayerRetrieveOptions(ChannelBehavior: PlayerChannelBehavior.Join));

            if (!resultPlayer.IsSuccess) {
                await FollowupAsync(GetErrorMessage(resultPlayer.Status));
                return;
            }

            if (resultPlayer.Player.VoiceChannelId != voiceChannelId) {
                await FollowupAsync("Bot já esta em uso em outro canal de voz");
                return;
            }

            var player = resultPlayer.Player;
            
            var isPlaylist = resultTracks.Playlist is not null;

            if (!isPlaylist) {
                await player.Queue.AddAsync(new TrackQueueItem(validTracks[0]));

                if (player.State == PlayerState.NotPlaying) {
                    await player.SkipAsync();
                    await FollowupAsync($"Tocando agora: **{validTracks[0].Title}**");
                } else {
                    await FollowupAsync($"Adicionado na fila: **{validTracks[0].Title}**");
                }
            } else {
                await player.Queue.AddRangeAsync(
                    validTracks.Select(t => new TrackQueueItem(t)).ToList()
                );

                if (player.State == PlayerState.NotPlaying) await player.SkipAsync();

                var playlistName = resultTracks.Playlist?.Name ?? "Desconhecida";
                await FollowupAsync($"Playlist **{playlistName}** adicionada com {validTracks.Count} músicas");
            }
        } catch (Exception e) {
            await FollowupAsync("Erro ao tentar buscar uma música a partir deste nome/link");
            logger.LogError(e, "Erro ao tentar buscar musica com query {Query}", query);
        }
    }

    private static string GetErrorMessage(PlayerRetrieveStatus retrieveStatus) =>
        retrieveStatus switch {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "Você não está conectado em um canal de voz",
            PlayerRetrieveStatus.BotNotConnected => "Bot não está conectado",
            _ => "Erro desconhecido",
        };
}