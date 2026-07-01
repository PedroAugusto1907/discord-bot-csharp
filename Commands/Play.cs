using DiscordBot.Player;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot.Commands;

public class Play(IAudioService audioService, RestClient restClient, GatewayClient gatewayClient, ILogger<Play> logger)
    : ApplicationCommandModule<SlashCommandContext> {
    [SlashCommand("play", "Toca uma música ou playlist do youtube!")]
    public async Task PlayAsync([SlashCommandParameter(Description = "Nome | Link")] string query) {
        await RespondAsync(InteractionCallback.DeferredMessage(flags: MessageFlags.Ephemeral));

        if (Context.Guild is null) {
            await FollowupAsync("Este comando só pode ser usado dentro de um servidor");
            return;
        }
        
        ulong voiceChannelId;

        try {
            var voiceState = await Context.Guild.GetUserVoiceStateAsync(Context.User.Id);

            if (voiceState?.ChannelId is null) {
                await FollowupAsync("Você não está em um canal de voz");
                return;
            }

            voiceChannelId = voiceState.ChannelId.Value;
        } catch {
            await FollowupAsync("Você não está em um canal de voz");
            return;
        }
        
        if (audioService.Players.TryGetPlayer<CustomPlayer>(Context.Guild.Id, out var existingPlayer)
            && existingPlayer is not null) {
            if (existingPlayer.IsGhost) {
                logger.LogWarning("Player fantasma detectado na guild {GuildId}, descartando antes de continuar",
                    Context.Guild.Id);

                await existingPlayer.DisposeAsync();
            } else if (existingPlayer.VoiceChannelId != voiceChannelId) {
                await FollowupAsync("Bot já está em uso em outro canal de voz");
                return;
            }
        }
        
        List<LavalinkTrack> validTracks;
        TrackLoadResult resultTracks;

        try {
            resultTracks = await audioService.Tracks.LoadTracksAsync(query, TrackSearchMode.YouTube);
        } catch (Exception e) {
            logger.LogError(e, "Erro ao tentar buscar musica com query {Query}", query);
            await FollowupAsync("Erro ao tentar buscar uma música a partir deste nome/link");
            return;
        }

        if (!resultTracks.HasMatches) {
            await FollowupAsync("Nenhuma música encontrada");
            return;
        }

        validTracks = resultTracks.Tracks.Where(t => !t.IsLiveStream).ToList();

        if (validTracks.Count == 0) {
            await FollowupAsync("Nenhuma música válida encontrada. OBS: Livestreams não são suportadas!");
            return;
        }
        
        PlayerResult<CustomPlayer> resultPlayer;

        try {
            resultPlayer = await audioService.Players.RetrieveAsync(Context.Guild.Id,
                voiceChannelId,
                playerFactory: PlayerFactory.Create<CustomPlayer, CustomPlayerOptions>(properties =>
                    new CustomPlayer(properties, restClient, gatewayClient,
                        properties.ServiceProvider!.GetRequiredService<ILogger<CustomPlayer>>())),
                options: Options.Create(new CustomPlayerOptions { TextChannelId = Context.Channel.Id }),
                retrieveOptions: new PlayerRetrieveOptions(ChannelBehavior: PlayerChannelBehavior.Join));
        } catch (Exception e) {
            logger.LogError(e, "Erro ao conectar o player na guild {GuildId}", Context.Guild.Id);
            await FollowupAsync("Erro ao tentar conectar no canal de voz");
            return;
        }

        if (!resultPlayer.IsSuccess) {
            await FollowupAsync(GetErrorMessage(resultPlayer.Status));
            return;
        }

        var player = resultPlayer.Player;
        
        await player.Lock.WaitAsync();

        try {
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
            logger.LogError(e, "Erro ao adicionar musica(s) na fila da guild {GuildId}", Context.Guild.Id);
            await FollowupAsync("Erro ao adicionar a música na fila");
        } finally {
            player.Lock.Release();
        }
    }

    private static string GetErrorMessage(PlayerRetrieveStatus retrieveStatus) =>
        retrieveStatus switch {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "Você não está conectado em um canal de voz",
            PlayerRetrieveStatus.VoiceChannelMismatch => "Bot já está em uso em outro canal de voz",
            PlayerRetrieveStatus.UserInSameVoiceChannel => "Você já está no mesmo canal de voz que o bot",
            PlayerRetrieveStatus.BotNotConnected => "Bot não está conectado",
            PlayerRetrieveStatus.PreconditionFailed => "Não foi possível iniciar o player neste momento",
            _ => "Erro desconhecido",
        };
}