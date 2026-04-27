using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Protocol.Payloads.Events;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;

namespace DiscordBot.Player;

public class CustomPlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties, RestClient restClient, ILogger<CustomPlayer> logger) : QueuedLavalinkPlayer(properties) {
    private ulong _textChannelId = properties.Options.Value.TextChannelId;

    public ulong? CurrentMessageId { get; private set; }

    private readonly SemaphoreSlim _lock = new(1, 1);

    public SemaphoreSlim Lock => _lock;

    public bool IsManualSkip { get; set; }

    private int _disposedFlag = 0;
    public bool IsDisposed => _disposedFlag == 1;

    protected override async ValueTask NotifyTrackStartedAsync(ITrackQueueItem trackItem, CancellationToken cancellationToken = default) {
        await base.NotifyTrackStartedAsync(trackItem, cancellationToken);

        if (RepeatMode == TrackRepeatMode.Track && !IsManualSkip) {
            await UpdateControlsAsync();
            return;
        }

        RepeatMode = TrackRepeatMode.None;
        IsManualSkip = false;

        var track = trackItem.Track;
        if (track is null) return;

        var duration = track.Duration == TimeSpan.MaxValue ? "∞" : FormatDuration(track.Duration);

        var embed = new EmbedProperties()
            .WithColor(new Color(0, 200, 100))
            .WithTitle("Tocando agora 🎵")
            .WithDescription($"[{track.Title} | {duration}]({track.Uri})")
            .WithThumbnail(new EmbedThumbnailProperties(track.ArtworkUri?.ToString() ?? ""));

        var msg = await restClient.SendMessageAsync(_textChannelId,
            new MessageProperties()
                .WithEmbeds([embed])
                .WithComponents([BuildControlRow()]), cancellationToken: cancellationToken);

        CurrentMessageId = msg.Id;

        if (IsDisposed) {
            await DisableControlsAsync();
        }
    }

    protected override async ValueTask
        NotifyTrackEndedAsync(ITrackQueueItem trackItem, TrackEndReason endReason, CancellationToken cancellationToken = default) {
        await base.NotifyTrackEndedAsync(trackItem, endReason, cancellationToken);

        if (RepeatMode == TrackRepeatMode.Track && !IsManualSkip) {
            return;
        }

        await DisableControlsAsync();
    }

    protected override async ValueTask DisposeAsyncCore() {
        if (Interlocked.Exchange(ref _disposedFlag, 1) == 1) return;

        await base
            .DisposeAsyncCore()
            .ConfigureAwait(false);

        await DisableControlsAsync();
    }

    public ActionRowProperties BuildControlRow() {
        var pauseStyle = State == PlayerState.Paused ? ButtonStyle.Success : ButtonStyle.Primary;
        var loopStyle = RepeatMode == TrackRepeatMode.Track ? ButtonStyle.Success : ButtonStyle.Primary;

        return new ActionRowProperties().WithComponents([
            new ButtonProperties("bt-skip", EmojiProperties.Standard("⏭️"), ButtonStyle.Primary),
            new ButtonProperties("bt-pausePlay", EmojiProperties.Standard("⏯️"), pauseStyle),
            new ButtonProperties("bt-shuffle", EmojiProperties.Standard("🔀"), ButtonStyle.Primary),
            new ButtonProperties("bt-loop", EmojiProperties.Standard("🔁"), loopStyle),
            new ButtonProperties("bt-stop", EmojiProperties.Standard("⏹️"), ButtonStyle.Danger),
        ]);
    }

    public async Task UpdateControlsAsync() {
        if (CurrentMessageId is null) return;

        await restClient.ModifyMessageAsync(_textChannelId, CurrentMessageId.Value, msg => msg.WithComponents([BuildControlRow()]));
    }

    public async Task DisableControlsAsync() {
        if (CurrentMessageId is null) return;

        try {
            await restClient.ModifyMessageAsync(_textChannelId, CurrentMessageId.Value, msg => msg.WithComponents([]));
        } catch (Exception e) {
            logger.LogError(e, "Não foi possível remover botões da mensagem {MessageId}", CurrentMessageId);
        } finally {
            CurrentMessageId = null;
        }
    }

    private static string FormatDuration(TimeSpan d) {
        if (d.Hours > 0) return $"{d.Hours}h {d.Minutes}m {d.Seconds}s";
        if (d.Minutes > 0) return $"{d.Minutes}m {d.Seconds}s";
        return $"{d.Seconds}s";
    }
}