namespace AdminShell.Contracts;

/// <summary>
/// Describes an action contributed to the shell header or page toolbar.
/// </summary>
public record HeaderActionDescriptor
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Button label / tooltip.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Element Plus icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Action target: 'header' (top bar), 'page.toolbar' (page-specific toolbar).
    /// For page-specific, add target page route in TargetPage.
    /// </summary>
    public string Target { get; init; } = "header";

    /// <summary>Target page route (only when Target = 'page.toolbar').</summary>
    public string? TargetPage { get; init; }

    /// <summary>
    /// Action type: 'route' (navigate), 'modal' (open modal), 'api' (call endpoint), 'emit' (emit event).
    /// </summary>
    public string ActionType { get; init; } = "route";

    /// <summary>
    /// Value depends on ActionType:
    /// - 'route': route path to navigate to
    /// - 'modal': modal component path
    /// - 'api': API endpoint URL
    /// - 'emit': event name to emit on the event bus
    /// </summary>
    public string? ActionValue { get; init; }

    /// <summary>Sort order among actions.</summary>
    public int Order { get; init; } = 100;

    /// <summary>Required permissions.</summary>
    public string[] RequiredPermissions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Plugin that contributes actions to the header or page toolbars.
/// </summary>
public interface IHeaderActionPlugin : IAdminShellPlugin
{
    /// <summary>
    /// Returns header/toolbar actions.
    /// </summary>
    IEnumerable<HeaderActionDescriptor> GetHeaderActions();
}