namespace AdminShell.Contracts;

/// <summary>
/// Describes a report type contributed by a plugin.
/// </summary>
public record ReportDescriptor
{
    /// <summary>Unique report ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Report description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Element Plus icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Report category for grouping (e.g., "users", "system", "audit").</summary>
    public string Category { get; init; } = "general";

    /// <summary>
    /// Supported output formats.
    /// </summary>
    public string[] SupportedFormats { get; init; } = new[] { "json", "csv" };

    /// <summary>
    /// Plugin endpoint that generates this report.
    /// Called with GET {ReportEndpoint}?format={format}&from={date}&to={date}
    /// </summary>
    public string ReportEndpoint { get; init; } = string.Empty;

    /// <summary>Sort order.</summary>
    public int Order { get; init; } = 100;

    /// <summary>Required permissions to view this report.</summary>
    public string[] RequiredPermissions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Plugin that contributes report types to the reporting module.
/// </summary>
public interface IReportPlugin : IPluginComponent
{
    /// <summary>
    /// Returns available report types.
    /// </summary>
    IEnumerable<ReportDescriptor> GetReports();
}