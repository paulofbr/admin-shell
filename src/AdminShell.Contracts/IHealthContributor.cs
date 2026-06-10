namespace AdminShell.Contracts;

/// <summary>
/// Allows plugins to contribute health check status to the system's health endpoint.
/// </summary>
public interface IHealthContributor
{
    /// <summary>
    /// Display name for this health check (e.g., "Reporting Database", "Audit Log").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs a health check and returns the result.
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of a health check contributed by a plugin.
/// </summary>
public record HealthCheckResult(
    string Name,
    string Status,
    string Description,
    TimeSpan Duration,
    string? ErrorMessage = null,
    IReadOnlyDictionary<string, object>? Data = null
);