using AdminShell.Contracts;

namespace AdminShell.Infrastructure.PluginSystem;

/// <summary>
/// In-memory registry for SQL queries contributed by data plugins.
/// </summary>
public sealed class PluginQueryRegistry : IQueryRegistry
{
    private readonly Dictionary<string, string> _queries = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string key, string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        _queries[key] = sql;
    }

    public string? GetQuery(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _queries.TryGetValue(key, out var sql) ? sql : null;
    }

    public IEnumerable<string> ListQueries()
    {
        return _queries.Keys.OrderBy(k => k).ToList();
    }
}
