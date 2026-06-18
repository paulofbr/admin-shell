using PluginName.Entities;
using PluginName.Repositories;
using PluginName.Services;

namespace PluginName.Services;

public sealed class PluginNameItemService : IPluginNameItemService
{
    private readonly IPluginNameItemRepository _repository;

    public PluginNameItemService(IPluginNameItemRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<PluginNameItem>> ListAsync(CancellationToken ct = default)
        => _repository.ListAsync(ct);
}
