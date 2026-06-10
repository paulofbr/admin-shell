namespace AdminShell.Contracts;

/// <summary>
/// Describes a loaded plugin and its state.
/// </summary>
public record PluginDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string Description { get; init; } = string.Empty;
    public string AssemblyPath { get; init; } = string.Empty;
    public PluginStatus Status { get; init; } = PluginStatus.Discovered;
    public IReadOnlyList<PluginDependencyInfo> Dependencies { get; init; } = Array.Empty<PluginDependencyInfo>();
    public DateTime LoadedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum PluginStatus
{
    Discovered,
    Loading,
    Loaded,
    Initializing,
    Active,
    Failed,
    Disabled
}

public record PluginDependencyInfo
{
    public string PluginId { get; init; } = string.Empty;
    public string? VersionConstraint { get; init; }
    public bool IsOptional { get; init; }
    public bool IsResolved { get; init; }
    public string? ErrorMessage { get; init; }
}
