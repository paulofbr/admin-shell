using Microsoft.AspNetCore.Routing;

namespace AdminShell.Contracts;

/// <summary>
/// Helper methods that keep plugin endpoint registration short and consistent.
/// </summary>
public static class PluginEndpointExtensions
{
    public static RouteGroupBuilder MapPluginApi(this IEndpointRouteBuilder endpoints, string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        return endpoints.MapGroup($"/api/plugins/{pluginId}");
    }
}
