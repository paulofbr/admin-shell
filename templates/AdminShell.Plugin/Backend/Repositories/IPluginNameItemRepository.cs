using PluginName.Entities;

namespace PluginName.Repositories;

public interface IPluginNameItemRepository
{
    Task<IReadOnlyList<PluginNameItem>> ListAsync(CancellationToken ct = default);
}
