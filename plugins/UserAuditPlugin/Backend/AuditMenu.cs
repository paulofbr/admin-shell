using AdminShell.Contracts;

namespace UserAuditPlugin;

[PluginComponent("user-audit")]
public sealed class AuditMenu : IMenuPlugin
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        yield return new MenuItem
        {
            Id = "audit",
            Label = "Audit Log",
            Icon = "List",
            Path = "/audit",
            Order = 40
        };
    }
}
