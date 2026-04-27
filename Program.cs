using DiscordBot.Config;
using DiscordBot.Repository;
using DiscordBot.Services;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;
using Lavalink4NET.Extensions;
using Lavalink4NET.NetCord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/bot-.log", rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try {
    Log.Information("Iniciando bot...");

    var builder = Host
        .CreateDefaultBuilder(args)
        .UseSerilog()
        .UseDiscordGateway(opt => {
            opt.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates | GatewayIntents.MessageContent;
        })
        .UseApplicationCommands<SlashCommandInteraction, SlashCommandContext>()
        .UseApplicationCommands<MessageCommandInteraction,  MessageCommandContext>()
        .UseComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
        .ConfigureServices((ctx, services) => {
            services.AddGatewayHandlers(typeof(Program).Assembly);
            services.AddSingleton<ActivityService>();
            services.AddSingleton<GifRepository>();
            services.AddLavalink();

            services.ConfigureLavalink(config => {
                var cfg = ctx
                    .Configuration.GetSection("Lavalink")
                    .Get<LavalinkConfig>() ?? throw new Exception("Config Lavalink não encontrada");

                config.BaseAddress = new Uri(cfg.Host);
                config.Passphrase = cfg.Senha;
                config.HttpClientName = cfg.Identificador;
                config.ReadyTimeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds);
            });
        });

    var host = builder.Build();

    host.AddModules(typeof(Program).Assembly);

    await host.RunAsync();
} catch (Exception ex) {
    Log.Fatal(ex, "Bot morreu inesperadamente");
} finally {
    Log.CloseAndFlush();
}