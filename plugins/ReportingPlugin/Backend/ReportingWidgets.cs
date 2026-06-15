using AdminShell.Contracts;

namespace ReportingPlugin;

[PluginComponent("reporting")]
public sealed class ReportingWidgets : IWidgetPlugin
{
    public IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor
        {
            Id = "reporting-summary",
            Title = "Reports Summary",
            Zone = "dashboard",
            Width = 4,
            Height = 3,
            Order = 10,
            Settings = new Dictionary<string, object>
            {
                ["refreshInterval"] = 60,
                ["showCharts"] = true
            }
        };

        yield return new WidgetDescriptor
        {
            Id = "reporting-recent",
            Title = "Recent Reports",
            Zone = "dashboard",
            Width = 4,
            Height = 4,
            Order = 20
        };
    }
}
