using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IPluginRepository : IBaseRepository<PluginInfo>
{
    Task<PluginInfo?> GetByPluginIdAsync(string pluginId, CancellationToken ct = default);
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
}
