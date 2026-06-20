using AdminShell.Contracts;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.PluginSystem;

public class PluginDependencyResolver
{
    private readonly ILogger _logger;

    public PluginDependencyResolver(ILogger logger)
    {
        _logger = logger;
    }

    public List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)> Resolve(
        List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)> instances)
    {
        var available = instances
            .GroupBy(p => p.Descriptor.Id)
            .ToDictionary(g => g.Key, g => g.First().Descriptor.Version);

        var resolved = new HashSet<string>();
        var ordered = new List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)>();

        void Visit((IAdminShellPlugin Instance, PluginDescriptor Descriptor) item)
        {
            if (resolved.Contains(item.Descriptor.Id)) return;
            resolved.Add(item.Descriptor.Id);

            var descriptor = item.Descriptor;
            var dependencies = descriptor.Dependencies.ToList();

            for (var i = 0; i < dependencies.Count; i++)
            {
                var dep = dependencies[i];

                if (available.TryGetValue(dep.PluginId, out var availableVersion))
                {
                    var isResolved = IsVersionSatisfied(availableVersion, dep.VersionConstraint);
                    dep = dep with
                    {
                        VersionConstraint = dep.VersionConstraint,
                        Version = dep.Version ?? dep.VersionConstraint,
                        IsResolved = isResolved,
                        ErrorMessage = isResolved
                            ? null
                            : $"Dependency {dep.PluginId} version {availableVersion} does not satisfy {dep.VersionConstraint}"
                    };

                    if (!isResolved)
                    {
                        descriptor = descriptor with
                        {
                            Status = PluginStatus.Failed,
                            ErrorMessage = dep.ErrorMessage
                        };
                    }
                }
                else
                {
                    dep = dep with
                    {
                        VersionConstraint = dep.VersionConstraint,
                        Version = dep.Version ?? dep.VersionConstraint,
                        IsResolved = dep.IsOptional,
                        ErrorMessage = dep.IsOptional
                            ? $"Optional dependency {dep.PluginId} is not installed"
                            : $"Missing required dependency {dep.PluginId}"
                    };

                    if (!dep.IsOptional)
                    {
                        descriptor = descriptor with
                        {
                            Status = PluginStatus.Failed,
                            ErrorMessage = dep.ErrorMessage
                        };
                    }
                }

                dependencies[i] = dep;

                var depItem = instances.FirstOrDefault(p => p.Descriptor.Id == dep.PluginId);
                if (depItem.Instance != null && dep.IsResolved)
                {
                    Visit(depItem);
                }
            }

            descriptor = descriptor with { Dependencies = dependencies };
            ordered.Add((item.Instance, descriptor));
        }

        foreach (var item in instances)
        {
            Visit(item);
        }

        return ordered;
    }

    internal static bool IsVersionSatisfied(string installedVersion, string? constraint)
    {
        if (string.IsNullOrWhiteSpace(constraint) || constraint.Trim() == "*")
            return true;

        var trimmed = constraint.Trim();
        var op = ">=";
        var required = trimmed;

        foreach (var candidate in new[] { ">=", "<=", "!=", ">", "<", "=" })
        {
            if (trimmed.StartsWith(candidate, StringComparison.Ordinal))
            {
                op = candidate;
                required = trimmed[candidate.Length..].Trim();
                break;
            }
        }

        if (!Version.TryParse(installedVersion, out var installed) ||
            !Version.TryParse(required, out var target))
        {
            return string.Equals(installedVersion, required, StringComparison.OrdinalIgnoreCase);
        }

        return op switch
        {
            ">=" => installed >= target,
            "<=" => installed <= target,
            ">" => installed > target,
            "<" => installed < target,
            "!=" => installed != target,
            _ => installed == target,
        };
    }
}
