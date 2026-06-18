using AdminShell.Contracts;
using AdminShell.Core.Entities;

namespace AdminShell.Infrastructure.Data;

public sealed class CoreManagedEntityProvider : IManagedEntityProvider
{
    public IEnumerable<Type> GetManagedEntityTypes()
    {
        yield return typeof(Role);
        yield return typeof(User);
        yield return typeof(PluginInfo);
        yield return typeof(Permission);
        yield return typeof(AppSetting);
        yield return typeof(AuditLog);
    }
}
