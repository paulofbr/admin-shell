namespace AdminShell.Contracts;

/// <summary>
/// Represents a script or style resource contributed by a plugin.
/// </summary>
public record PageResourceDescriptor
{
    /// <summary>Resource type.</summary>
    public string Type { get; init; } = "script"; // "script" or "style"

    /// <summary>URL path to the resource (served by the plugin's static files).</summary>
    public string Src { get; init; } = string.Empty;

    /// <summary>Pages where this resource should load. Empty = all pages.</summary>
    public string[] IncludePages { get; init; } = Array.Empty<string>();

    /// <summary>Position: 'head' (in head tag) or 'body' (at end of body).</summary>
    public string Position { get; init; } = "body"; // "head" or "body"
}

/// <summary>
/// Allows a plugin to inject custom JavaScript, CSS, or HTML into shell pages.
/// Useful for plugins that need custom client-side logic without creating full pages.
/// </summary>
public interface IPageExtensionPlugin : IAdminShellPlugin
{
    /// <summary>
    /// Returns scripts and styles to inject into shell pages.
    /// </summary>
    IEnumerable<PageResourceDescriptor> GetPageResources();
}