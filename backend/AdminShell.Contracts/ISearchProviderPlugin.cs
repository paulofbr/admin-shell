namespace AdminShell.Contracts;

/// <summary>
/// A single search result item.
/// </summary>
public record SearchResultItem
{
    /// <summary>Result title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Result description / preview.</summary>
    public string? Description { get; init; }

    /// <summary>Route URL to navigate to.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Category label (e.g., "Users", "Settings", "Reports").</summary>
    public string Category { get; init; } = "General";

    /// <summary>Element Plus icon name for the result.</summary>
    public string? Icon { get; init; }

    /// <summary>Relevance score (0-100). Higher = more relevant.</summary>
    public int Score { get; init; } = 50;
}

/// <summary>
/// Plugin that contributes search results to the global search.
/// </summary>
public interface ISearchProviderPlugin : IPluginComponent
{
    /// <summary>
    /// Returns search results matching the query.
    /// </summary>
    /// <param name="query">User's search query string.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IEnumerable<SearchResultItem>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
}