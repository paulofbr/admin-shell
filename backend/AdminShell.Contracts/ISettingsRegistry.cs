namespace AdminShell.Contracts;

public interface ISettingsRegistry
{
    IReadOnlyList<SettingDefinition> GetAll();
    IReadOnlyList<SettingDefinition> GetForCategory(string category);
    Task EnsureDefaultsAsync(CancellationToken ct = default);
    Task<SettingsResponse> GetSettingsAsync(string category, CancellationToken ct = default);
    Task<SettingsResponse> UpdateAsync(
        string category,
        IEnumerable<UpdateSettingRequest> requests,
        CancellationToken ct = default);
    Task<TSettings> GetOptionsAsync<TSettings>(CancellationToken ct = default) where TSettings : class, new();
    Task SetOptionsAsync<TSettings>(TSettings options, CancellationToken ct = default) where TSettings : class;
}
