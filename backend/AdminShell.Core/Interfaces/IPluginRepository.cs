using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IPluginRepository
{
    Task<PluginInfo?> GetByPluginIdAsync(string pluginId, CancellationToken ct = default);
    Task<IReadOnlyList<PluginInfo>> GetAllAsync(CancellationToken ct = default);
    Task<PluginInfo> AddAsync(PluginInfo plugin, CancellationToken ct = default);
    Task UpdateAsync(PluginInfo plugin, CancellationToken ct = default);
    Task DeleteAsync(PluginInfo plugin, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
}
