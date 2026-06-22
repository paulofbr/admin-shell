using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class PluginRepository : RepositoryBase<PluginInfo>, IPluginRepository
{
    public PluginRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
        : base(connectionFactory, extensionRegistry)
    {
    }

    public async Task<PluginInfo?> GetByPluginIdAsync(string pluginId, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<PluginInfo>(
            "SELECT * FROM PluginInfos WHERE PluginId = @PluginId AND IsDeleted = 0",
            new { PluginId = pluginId });
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PluginInfos WHERE IsDeleted = 0 AND IsEnabled = 1");
    }
}