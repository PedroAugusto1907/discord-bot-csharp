using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Repository;

public class GifRepository(IConfiguration config, ILogger<GifRepository> logger) {
    private readonly string _gifsPath = Path.Combine(AppContext.BaseDirectory, "gifs.json");

    private string FallbackGif = config["Gifs:Fallback"] ?? throw new InvalidOperationException("Gifs:Fallback não configurado");

    private IReadOnlyList<string>? _source;
    private Queue<string> _pool = new();
    private readonly Lock _lock = new();

    public int SourceCount => _source?.Count ?? 0;
    public int PoolCount => _pool.Count;

    public string GetGif() {
        lock (_lock) {
            if (_pool.Count == 0) Refill();
            return _pool.TryDequeue(out var gif) ? gif : FallbackGif;
        }
    }

    public async Task<bool> RemoveAsync(string url) {
        lock (_lock) {
            if (_source is null || !_source.Contains(url)) return false;

            _source = _source
                .Where(u => u != url)
                .ToList();

            _pool = new Queue<string>(_pool.Where(u => u != url));

            logger.LogInformation("GifRepository: URL removida — {Url}", url);
        }
        
        await File.WriteAllTextAsync(_gifsPath, JsonSerializer.Serialize(_source, new JsonSerializerOptions { WriteIndented = true }));

        return true;
    }

    public async Task Load() {
        if (!File.Exists(_gifsPath)) {
            logger.LogError("GifRepository: arquivo {Path} não encontrado", _gifsPath);
            _source = [];
            return;
        }

        try {
            var json = await File.ReadAllTextAsync(_gifsPath);
            _source = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            logger.LogInformation("GifRepository: {Count} GIFs carregados de {Path}", _source.Count, _gifsPath);
        } catch (Exception ex) {
            logger.LogError(ex, "GifRepository: erro ao carregar {Path}", _gifsPath);
            _source = [];
        }

        lock (_lock) {
            Refill();
        }
    }

    private void Refill() {
        if (_source is null || _source.Count == 0) return;

        var shuffled = _source.ToArray();
        Random.Shared.Shuffle(shuffled);
        _pool = new Queue<string>(shuffled);
        logger.LogDebug("GifRepository: pool reabastecido com {Count} GIFs", _pool.Count);
    }
}