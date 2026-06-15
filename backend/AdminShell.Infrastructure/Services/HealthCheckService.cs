using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Services;

public interface IHealthCheckService
{
    Task<IReadOnlyList<HealthCheckResult>> CheckAllAsync(CancellationToken ct = default);
}

public class HealthCheckService : IHealthCheckService
{
    private readonly IPluginLoader _pluginLoader;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(IPluginLoader pluginLoader, ILogger<HealthCheckService> logger)
    {
        _pluginLoader = pluginLoader;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HealthCheckResult>> CheckAllAsync(CancellationToken ct = default)
    {
        var results = new List<HealthCheckResult>();

        // Built-in checks
        results.Add(new HealthCheckResult
        (
            Name: "System",
            Status: "Healthy",
            Description: $"System running. {_pluginLoader.LoadedPlugins.Count} plugins loaded.",
            Duration: TimeSpan.Zero
        ));

        // Plugin health checks (plugins that implement IHealthContributor)
        foreach (var instance in _pluginLoader.GetPluginInstances())
        {
            if (instance is IHealthContributor contributor)
            {
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = await contributor.CheckHealthAsync(ct);
                    sw.Stop();
                    results.Add(result with { Duration = sw.Elapsed });
                }
                catch (Exception ex)
                {
                    results.Add(new HealthCheckResult
                    (
                        Name: contributor.Name,
                        Status: "Unhealthy",
                        Description: ex.Message,
                        Duration: TimeSpan.Zero,
                        ErrorMessage: ex.Message
                    ));
                    _logger.LogError(ex, "Health check failed for {Contributor}", contributor.Name);
                }
            }
        }

        return results;
    }
}