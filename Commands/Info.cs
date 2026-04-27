using DiscordBot.Repository;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot.Commands;

public class Info(GifRepository gifRepository) : ApplicationCommandModule<SlashCommandContext> {
    [SlashCommand("info", "Exibe informações sobre o bot")]
    public async Task InfoAsync() {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var uptime = DateTimeOffset.UtcNow - process.StartTime;
        var memoryMb = process.WorkingSet64 / 1024 / 1024;

        var embed = new EmbedProperties()
            .WithTitle("🔧 Bot — Informações Técnicas")
            .WithColor(new NetCord.Color(0x5865F2))
            .AddFields(new EmbedFieldProperties()
                    .WithName("Runtime")
                    .WithValue($".NET {Environment.Version}")
                    .WithInline(true),
                new EmbedFieldProperties()
                    .WithName("Memória")
                    .WithValue($"{memoryMb} MB")
                    .WithInline(true),
                new EmbedFieldProperties()
                    .WithName("Framework")
                    .WithValue("NetCord + Lavalink4NET")
                    .WithInline(true),
                new EmbedFieldProperties()
                    .WithName("Uptime")
                    .WithValue($"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s")
                    .WithInline(true),
                new EmbedFieldProperties()
                    .WithName("GIFs totais")
                    .WithValue($"{gifRepository.SourceCount}")
                    .WithInline(true),
                new EmbedFieldProperties()
                    .WithName("GIFs fila")
                    .WithValue($"{gifRepository.PoolCount}")
                    .WithInline(true));

         await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties().AddEmbeds(embed)));
    }
}