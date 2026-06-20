using AdminShell.Contracts;

namespace AdminShell.Infrastructure.PluginSystem;

public sealed class SettingsAccessor<TSettings> : ISettingsAccessor<TSettings>
    where TSettings : class, new()
{
    private readonly ISettingsRegistry _registry;

    public SettingsAccessor(ISettingsRegistry registry)
    {
        _registry = registry;
    }

    public Task<TSettings> GetAsync(CancellationToken ct = default)
        => _registry.GetOptionsAsync<TSettings>(ct);
}
