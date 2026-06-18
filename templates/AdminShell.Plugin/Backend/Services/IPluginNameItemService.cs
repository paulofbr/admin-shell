using PluginName.Entities;

namespace PluginName.Services;

public interface IPluginNameItemService
{
    Task<IReadOnlyList<PluginNameItem>> ListAsync(CancellationToken ct = default);
}
