using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class PluginRepository : IPluginRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PluginRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PluginInfo?> GetByPluginIdAsync(string pluginId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.QueryFirstOrDefaultAsync<PluginInfo>(
            "SELECT * FROM PluginInfos WHERE PluginId = @PluginId AND IsDeleted = 0",
            new { PluginId = pluginId });
    }

    public async Task<IReadOnlyList<PluginInfo>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var plugins = await db.QueryAsync<PluginInfo>(
            "SELECT * FROM PluginInfos WHERE IsDeleted = 0 ORDER BY Name");
        return plugins.ToList();
    }

    public async Task<PluginInfo> AddAsync(PluginInfo plugin, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            @"INSERT INTO PluginInfos (Id, PluginId, Name, Version, AssemblyPath, IsEnabled, Description, SettingsJson, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Id, @PluginId, @Name, @Version, @AssemblyPath, @IsEnabled, @Description, @SettingsJson, 0, @CreatedAt, @CreatedBy)",
            new { plugin.Id, plugin.PluginId, plugin.Name, plugin.Version, plugin.AssemblyPath,
                  plugin.IsEnabled, plugin.Description, plugin.SettingsJson, plugin.CreatedAt, plugin.CreatedBy });
        return plugin;
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PluginInfos WHERE IsDeleted = 0");
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PluginInfos WHERE IsDeleted = 0 AND IsEnabled = 1");
    }

    public async Task UpdateAsync(PluginInfo plugin, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            @"UPDATE PluginInfos SET
                Name = @Name, Version = @Version, AssemblyPath = @AssemblyPath,
                IsEnabled = @IsEnabled, Description = @Description, SettingsJson = @SettingsJson
              WHERE Id = @Id",
            new { plugin.Id, plugin.Name, plugin.Version, plugin.AssemblyPath,
                  plugin.IsEnabled, plugin.Description, plugin.SettingsJson });
    }

    public async Task DeleteAsync(PluginInfo plugin, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "UPDATE PluginInfos SET IsDeleted = 1, DeletedAt = @DeletedAt WHERE Id = @Id",
            new { plugin.Id, DeletedAt = DateTime.UtcNow });
    }
}