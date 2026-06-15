using AdminShell.Contracts;

namespace ReportingPlugin;

[PluginComponent("reporting")]
public sealed class ReportingMenu : IMenuPlugin
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        yield return new MenuItem
        {
            Id = "reports",
            Label = "Reports",
            Path = "/reports",
            Icon = "Document",
            Order = 30
        };

        yield return new MenuItem
        {
            Id = "reports-dashboard",
            Label = "Dashboard",
            Path = "/reports/dashboard",
            Icon = "DataAnalysis",
            Order = 31,
            ParentId = "reports"
        };

        yield return new MenuItem
        {
            Id = "reports-analytics",
            Label = "Analytics",
            Path = "/reports/analytics",
            Icon = "TrendCharts",
            Order = 32,
            ParentId = "reports"
        };
    }
}
