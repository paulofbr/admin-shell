using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface ISettingsRepository : IBaseRepository<AppSetting>
{
    Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<AppSetting>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);
    Task<AppSetting> SetAsync(AppSetting setting, CancellationToken ct = default);
    Task SetBatchAsync(IEnumerable<AppSetting> settings, CancellationToken ct = default);
}
