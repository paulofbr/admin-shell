using System.Reflection;
using AdminShell.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.PluginSystem;

public class PluginServiceRegistrar
{
    private readonly ILogger _logger;
    private readonly HashSet<string> _initializedAssemblies = new(StringComparer.OrdinalIgnoreCase);

    public PluginServiceRegistrar(ILogger logger)
    {
        _logger = logger;
    }

    public void RegisterConventionalServices(IServiceCollection services, Assembly assembly)
    {
        var key = assembly.Location;
        if (!_initializedAssemblies.Add(key))
            return;

        foreach (var implementationType in assembly.GetTypes()
                     .Where(IsConventionalServiceOrRepositoryType)
                     .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase))
        {
            if (typeof(IApiPlugin).IsAssignableFrom(implementationType))
            {
                services.AddScoped(implementationType);
                _logger.LogDebug("Registered conventional API plugin type {Type}",
                    implementationType.FullName);
            }

            foreach (var serviceType in GetConventionalServiceInterfaces(implementationType)
                         .Where(st => st != typeof(IApiPlugin) && st != typeof(IPluginComponent)))
            {
                try
                {
                    services.AddScoped(serviceType, implementationType);
                    _logger.LogDebug("Registered conventional service {Service} -> {Impl}",
                        serviceType.FullName, implementationType.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register conventional service {Service} -> {Impl}",
                        serviceType.FullName, implementationType.FullName);
                }
            }
        }
    }

    private static bool IsConventionalServiceOrRepositoryType(Type type)
        => !type.IsAbstract
           && !type.IsInterface
           && !typeof(IAdminShellPlugin).IsAssignableFrom(type)
           && (IsInPluginSubNamespace(type, "Services")
               || IsInPluginSubNamespace(type, "Repositories")
               || IsInPluginSubNamespace(type, "Apis")
               || type.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase)
               || type.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase)
               || type.Name.EndsWith("Api", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Type> GetConventionalServiceInterfaces(Type implementationType)
    {
        var namespacePrefix = implementationType.Namespace ?? string.Empty;
        return implementationType.GetInterfaces()
            .Where(serviceType => serviceType.IsPublic
                                  && !serviceType.IsGenericTypeDefinition
                                  && serviceType.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) == true)
            .DefaultIfEmpty(implementationType)
            .Distinct()
            .OrderBy(serviceType => serviceType.FullName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsInPluginSubNamespace(Type type, string segment)
    {
        var ns = type.Namespace ?? string.Empty;
        return ns.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => part.Equals(segment, StringComparison.OrdinalIgnoreCase));
    }
}
