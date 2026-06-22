using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.Data.Repositories;

public class SettingsRepository : RepositoryBase<AppSetting>, ISettingsRepository
{
    public SettingsRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
        : base(connectionFactory, extensionRegistry)
    {
    }

    public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Key", key);
        ApplySoftDelete(query);
        return await qf.FirstOrDefaultAsync<AppSetting>(query, null, null, ct);
    }

    public async Task<IReadOnlyList<AppSetting>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Category", category).OrderBy("Key");
        ApplySoftDelete(query);
        return (await qf.GetAsync<AppSetting>(query, null, null, ct)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Select("Category").Distinct().OrderBy("Category");
        ApplySoftDelete(query);
        var rows = await qf.GetAsync<IDictionary<string, object?>>(query, null, null, ct);
        return rows.Select(r => Convert.ToString(r["Category"])!).ToList();
    }

    public async Task<AppSetting> SetAsync(AppSetting setting, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);

        var existing = await qf.FirstOrDefaultAsync<AppSetting>(
            new Query(TableName).Where("Key", setting.Key).Where("IsDeleted", 0),
            null, null, ct);

        if (existing != null)
        {
            existing.Value = setting.Value;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = setting.UpdatedBy ?? setting.CreatedBy;

            await qf.ExecuteAsync(
                new Query(TableName).Where("Id", existing.Id).AsUpdate(new Dictionary<string, object>
                {
                    ["Value"] = existing.Value,
                    ["UpdatedAt"] = existing.UpdatedAt,
                    ["UpdatedBy"] = existing.UpdatedBy
                }),
                null, null, ct);

            return existing;
        }
        else
        {
            setting.Id = Guid.NewGuid();
            setting.CreatedAt = DateTime.UtcNow;

            await qf.ExecuteAsync(
                new Query(TableName).AsInsert(new Dictionary<string, object>
                {
                    ["Id"] = setting.Id,
                    ["Key"] = setting.Key,
                    ["Value"] = setting.Value,
                    ["Category"] = setting.Category,
                    ["Description"] = (object?)setting.Description ?? DBNull.Value,
                    ["ValueType"] = setting.ValueType,
                    ["IsDeleted"] = 0,
                    ["CreatedAt"] = setting.CreatedAt,
                    ["CreatedBy"] = setting.CreatedBy
                }),
                null, null, ct);

            return setting;
        }
    }

    public async Task SetBatchAsync(IEnumerable<AppSetting> settings, CancellationToken ct = default)
    {
        foreach (var s in settings)
            await SetAsync(s, ct);
    }
}
