namespace AdminShell.Contracts;

/// <summary>
/// Represents a menu item contributed by a plugin.
/// </summary>
public record MenuItem
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Path { get; init; }
    public int Order { get; init; } = 100;
    public string? ParentId { get; init; }
    public string[]? Permissions { get; init; }
    public IReadOnlyList<MenuItem>? Children { get; init; }
}

/// <summary>
/// Plugin that contributes menu items.
/// </summary>
public interface IMenuPlugin : IPluginComponent
{
    IEnumerable<MenuItem> GetMenuItems();
}
