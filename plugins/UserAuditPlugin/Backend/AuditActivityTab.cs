using AdminShell.Contracts;

namespace UserAuditPlugin;

[PluginComponent("user-audit")]
public sealed class AuditActivityTab : ITabPlugin
{
    public IEnumerable<TabDescriptor> GetTabs()
    {
        yield return new TabDescriptor
        {
            Id = "user-activity",
            Label = "Activity",
            Icon = "Timer",
            TargetPage = "profile",
            Order = 20,
            ComponentPath = "UserActivityTab",
            RequiredPermissions = new[] { "audit:read", "users:read" }
        };
    }
}
