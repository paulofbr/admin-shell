using AdminShell.Contracts;

namespace UserAuditPlugin;

[PluginComponent("user-audit")]
public sealed class AuditPageResources : IPageExtensionPlugin
{
    public IEnumerable<PageResourceDescriptor> GetPageResources()
    {
        yield return new PageResourceDescriptor
        {
            Type = "style",
            Src = "/api/plugins/useraudit/resources/audit-badge.css",
            IncludePages = new[] { "/users" },
            Position = "head"
        };
    }
}
