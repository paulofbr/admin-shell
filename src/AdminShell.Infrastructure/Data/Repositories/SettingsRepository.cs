using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SettingsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.QueryFirstOrDefaultAsync<AppSetting>(
            "SELECT * FROM Settings WHERE [Key] = @Key AND IsDeleted = 0",
            new { Key = key });
    }

    public async Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var settings = await db.QueryAsync<AppSetting>(
            "SELECT * FROM Settings WHERE IsDeleted = 0 ORDER BY Category, [Key]");
        return settings.ToList();
    }

    public async Task<IReadOnlyList<AppSetting>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var settings = await db.QueryAsync<AppSetting>(
            "SELECT * FROM Settings WHERE Category = @Cat AND IsDeleted = 0 ORDER BY [Key]",
            new { Cat = category });
        return settings.ToList();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var cats = await db.QueryAsync<string>(
            "SELECT DISTINCT Category FROM Settings WHERE IsDeleted = 0 ORDER BY Category");
        return cats.ToList();
    }

    public async Task<AppSetting> SetAsync(AppSetting setting, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        var existing = await db.QueryFirstOrDefaultAsync<AppSetting>(
            "SELECT * FROM Settings WHERE [Key] = @Key AND IsDeleted = 0",
            new { setting.Key });

        if (existing != null)
        {
            // Update
            existing.Value = setting.Value;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = setting.UpdatedBy ?? setting.CreatedBy;

            await db.ExecuteAsync(
                "UPDATE Settings SET Value = @Value, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy WHERE Id = @Id",
                new { existing.Value, existing.UpdatedAt, existing.UpdatedBy, existing.Id });

            return existing;
        }
        else
        {
            // Insert
            setting.Id = Guid.NewGuid();
            setting.CreatedAt = DateTime.UtcNow;

            await db.ExecuteAsync(
                @"INSERT INTO Settings (Id, [Key], Value, Category, Description, ValueType, IsDeleted, CreatedAt, CreatedBy)
                  VALUES (@Id, @Key, @Value, @Category, @Description, @ValueType, 0, @CreatedAt, @CreatedBy)",
                new
                {
                    setting.Id, setting.Key, setting.Value, setting.Category,
                    setting.Description, setting.ValueType, setting.CreatedAt, setting.CreatedBy
                });

            return setting;
        }
    }

    public async Task SetBatchAsync(IEnumerable<AppSetting> settings, CancellationToken ct = default)
    {
        foreach (var s in settings)
            await SetAsync(s, ct);
    }
}