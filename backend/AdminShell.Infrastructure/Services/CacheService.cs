using System.Collections.Concurrent;
using AdminShell.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(5);
    private readonly Timer _cleanupTimer;

    public CacheService(ILogger<CacheService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            entry.LastAccessed = DateTime.UtcNow;
            return Task.FromResult(entry.Value as T);
        }

        if (entry is not null)
            _cache.TryRemove(key, out _);

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        _cache[key] = new CacheEntry(value, expiry ?? _defaultExpiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var keys = _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keys)
            _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private void Cleanup()
    {
        var expired = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
        {
            if (_cache.TryRemove(key, out _))
                _logger.LogTrace("Evicted expired cache entry: {Key}", key);
        }
    }

    private class CacheEntry
    {
        public object Value { get; }
        public DateTime ExpiresAt { get; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime LastAccessed { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public CacheEntry(object value, TimeSpan expiry)
        {
            Value = value;
            ExpiresAt = DateTime.UtcNow.Add(expiry);
        }
    }
}
