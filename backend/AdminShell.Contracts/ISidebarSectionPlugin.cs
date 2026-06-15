namespace AdminShell.Contracts;

/// <summary>
/// Describes a complete sidebar section contributed by a plugin.
/// A section is a group of related menu items with a header label.
/// </summary>
public record SidebarSectionDescriptor
{
    /// <summary>Unique section ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Section label shown as header (e.g., "Tools", "Analytics").</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Sort order among sections (lower = higher).</summary>
    public int Order { get; init; } = 100;

    /// <summary>Icon for the section header.</summary>
    public string? Icon { get; init; }

    /// <summary>Menu items within this section.</summary>
    public SidebarMenuItem[] Items { get; init; } = Array.Empty<SidebarMenuItem>();

    /// <summary>Collapsed by default.</summary>
    public bool Collapsed { get; init; }

    /// <summary>Required permissions to see this section.</summary>
    public string[] RequiredPermissions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// A single menu item within a sidebar section.
/// </summary>
public record SidebarMenuItem
{
    /// <summary>Item ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Display label.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Element Plus icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Route path to navigate to.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Sort order within the section.</summary>
    public int Order { get; init; } = 100;

    /// <summary>Required permissions.</summary>
    public string[] RequiredPermissions { get; init; } = Array.Empty<string>();

    /// <summary>Child items (submenu).</summary>
    public SidebarMenuItem[] Children { get; init; } = Array.Empty<SidebarMenuItem>();
}

/// <summary>
/// Plugin that contributes entire sidebar sections (groups of menu items).
/// Use this when a plugin needs more than just a single menu item.
/// </summary>
public interface ISidebarSectionPlugin : IPluginComponent
{
    /// <summary>
    /// Returns sidebar sections to add to the main navigation.
    /// </summary>
    IEnumerable<SidebarSectionDescriptor> GetSidebarSections();
}