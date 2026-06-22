using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

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
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("PluginId", pluginId);
        ApplySoftDelete(query);
        return await qf.FirstOrDefaultAsync<PluginInfo>(query, null, null, ct);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).AsCount().Where("IsDeleted", 0).Where("IsEnabled", 1);
        var result = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        return Convert.ToInt32(result?.Values.FirstOrDefault() ?? 0);
    }
}
