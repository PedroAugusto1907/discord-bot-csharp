# 🎵 Discord Music Bot

A Discord bot built with **C# / .NET 10** focused on music playback and server interaction features. Uses [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) for high-quality audio streaming and [NetCord](https://github.com/NetCordDev/NetCord) as the Discord library.

---

## ✨ Features

- **Music playback** — Search and play songs or full playlists from YouTube via slash commands
- **Interactive controls** — Skip, pause/resume, shuffle, loop and stop buttons on the now-playing message
- **Auto-disconnect** — Bot leaves the voice channel automatically when it's empty
- **GIF reactions** — React to messages with a random GIF from a curated pool (message context command)
- **Bot info** — Embed showing runtime, memory usage, uptime and framework via `/info`
- **Dynamic presence** — Status updates automatically as the bot joins or leaves servers

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Language | C# 14 / .NET 10 |
| Discord Library | NetCord |
| Audio Engine | Lavalink4NET 4.2 |
| Logging | Serilog |

---

## 📁 Project Structure

```
├── Commands/         # Slash commands and message commands
├── Config/           # Configuration models
├── Events/           # Discord gateway event handlers
├── Player/           # Custom Lavalink player with queue support
├── Repository/       # GIF data access layer
├── Services/         # Bot presence / activity service
└── Program.cs        # Entry point and DI setup
```

---

## ⚙️ Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A running [Lavalink](https://github.com/lavalink-devs/Lavalink) server
- A Discord bot token ([Discord Developer Portal](https://discord.com/developers/applications))

### Configuration

Fill in your values in `appsettings.json` (or create an `appsettings.Production.json` for production):

```json
{
  "Discord": {
    "Token": "YOUR_BOT_TOKEN"
  },
  "Lavalink": {
    "Host": "http://localhost:2333",
    "Identificador": "lavalink",
    "Senha": "youshallnotpass",
    "TimeoutSeconds": 10
  },
  "Bot": {
    "OwnerId": "YOUR_DISCORD_USER_ID",
    "AvatarFileName": "avatar.gif"
  },
  "Gifs": {
    "Fallback": "https://example.com/fallback.gif"
  }
}
```

> ⚠️ Never commit files with real tokens. Production settings are gitignored.

Add a `gifs.json` file in the output directory with a JSON array of GIF URLs:

```json
[
  "https://example.com/gif1.gif",
  "https://example.com/gif2.gif"
]
```


---

## 📋 Commands

| Command | Type | Description |
|---|---|---|
| `/play <query>` | Slash | Play a song or playlist from YouTube |
| `/dc` | Slash | Disconnect the bot from voice |
| `/info` | Slash | Show bot technical info |
| `Reagir com GIF` | Message | React to a message with a random GIF |
| `Remover GIF` | Message | Remove a GIF from the pool *(owner only)* |

---

## 📜 License

MIT
