namespace AdminShell.Contracts;

/// <summary>
/// Describes a tab contributed by a plugin to an existing page.
/// </summary>
public record TabDescriptor
{
    /// <summary>Unique identifier for this tab.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Display label shown on the tab.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Element Plus icon name (e.g., 'User', 'Setting').</summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Target page route where this tab appears.
    /// Built-in targets: 'users.detail', 'roles.detail', 'settings.detail', 'profile'
    /// Plugins can also target other plugin pages.
    /// </summary>
    public string TargetPage { get; init; } = "users.detail";

    /// <summary>Sort order among tabs (lower = first).</summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Frontend component path (Vue component) to render as tab content.
    /// Relative to the plugin's frontend directory.
    /// </summary>
    public string ComponentPath { get; init; } = string.Empty;

    /// <summary>
    /// Required permissions. User needs at least one of these to see the tab.
    /// Each entry is a resource:action string (e.g., "users:read").
    /// Empty array means visible to all authenticated users.
    /// </summary>
    public string[] RequiredPermissions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Plugin-specific props to pass to the component.
    /// </summary>
    public Dictionary<string, object>? Props { get; init; }
}

/// <summary>
/// Plugin that contributes tabs to existing shell pages.
/// </summary>
public interface ITabPlugin : IAdminShellPlugin
{
    /// <summary>
    /// Returns tabs that should be added to existing pages.
    /// </summary>
    IEnumerable<TabDescriptor> GetTabs();
}