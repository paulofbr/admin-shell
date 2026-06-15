using AdminShell.Contracts;

namespace ReportingPlugin;

[PluginComponent("reporting")]
public sealed class ReportingHeaderActions : IHeaderActionPlugin
{
    public IEnumerable<HeaderActionDescriptor> GetHeaderActions()
    {
        yield return new HeaderActionDescriptor
        {
            Id = "quick-report",
            Label = "New Report",
            Icon = "Plus",
            Target = "header",
            ActionType = "route",
            ActionValue = "/reports/create",
            Order = 10,
            RequiredPermissions = new[] { "reports:create" }
        };

        yield return new HeaderActionDescriptor
        {
            Id = "export-data",
            Label = "Export Data",
            Icon = "Download",
            Target = "page.toolbar",
            TargetPage = "/users",
            ActionType = "modal",
            ActionValue = "ExportDataModal",
            Order = 10,
            RequiredPermissions = new[] { "users:read" }
        };
    }
}
