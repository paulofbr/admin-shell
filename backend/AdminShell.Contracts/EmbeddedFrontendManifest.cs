namespace AdminShell.Contracts;

/// <summary>
/// Describes a frontend bundle embedded inside a backend plugin assembly.
/// </summary>
public record PluginDependencyDescriptor
{
    public string Id { get; init; } = "";
    public string Version { get; init; } = "";
}

public record EmbeddedFrontendManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string Description { get; init; } = "";
    public string Source { get; init; } = "embedded";
    public string Main { get; init; } = "index.js";
    public string[] Styles { get; init; } = [];
    public PluginDependencyDescriptor[] Dependencies { get; init; } = [];
}

/// <summary>
/// Static asset served from an embedded frontend bundle.
/// </summary>
public record EmbeddedFrontendAsset(string Path, string ContentType, byte[] Content);
