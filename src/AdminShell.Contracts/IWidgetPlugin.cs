namespace AdminShell.Contracts;

/// <summary>
/// Represents a widget contributed by a plugin.
/// </summary>
public record WidgetDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Zone { get; init; } = "dashboard";
    public int Order { get; init; } = 100;
    public int Width { get; init; } = 4;
    public int Height { get; init; } = 4;
    public string? ComponentName { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

/// <summary>
/// Plugin that contributes dashboard widgets.
/// </summary>
public interface IWidgetPlugin : IAdminShellPlugin
{
    IEnumerable<WidgetDescriptor> GetWidgets();
}
