using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.Data.Repositories;

public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    protected override string DefaultSortDir => "DESC";

    public AuditLogRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
        : base(connectionFactory, extensionRegistry)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(int skip, int take, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Skip(skip).Take(take).OrderByDesc("CreatedAt");
        ApplySoftDelete(query);
        return (await qf.GetAsync<AuditLog>(query, null, null, ct)).ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName)
            .Where("Action", action)
            .OrderByDesc("CreatedAt")
            .Skip(skip)
            .Take(take);
        ApplySoftDelete(query);
        return (await qf.GetAsync<AuditLog>(query, null, null, ct)).ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName)
            .Where("PerformedBy", userId)
            .OrderByDesc("CreatedAt")
            .Skip(skip)
            .Take(take);
        ApplySoftDelete(query);
        return (await qf.GetAsync<AuditLog>(query, null, null, ct)).ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to,
        int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName)
            .Where("CreatedAt", ">=", from)
            .Where("CreatedAt", "<=", to)
            .OrderByDesc("CreatedAt")
            .Skip(skip)
            .Take(take);
        ApplySoftDelete(query);
        return (await qf.GetAsync<AuditLog>(query, null, null, ct)).ToList();
    }

    public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).AsCount().Where("CreatedAt", ">=", since);
        ApplySoftDelete(query);
        var result = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        return Convert.ToInt32(result?.Values.FirstOrDefault() ?? 0);
    }

    public async Task<int> GetCountByActionSinceAsync(string action, DateTime since, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).AsCount().Where("Action", action).Where("CreatedAt", ">=", since);
        ApplySoftDelete(query);
        var result = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        return Convert.ToInt32(result?.Values.FirstOrDefault() ?? 0);
    }

    public async Task<List<AuditActionCount>> GetCountByActionGroupAsync(DateTime since, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName)
            .Select("Action")
            .SelectRaw("COUNT(*) AS Count")
            .Where("CreatedAt", ">=", since)
            .GroupBy("Action")
            .OrderByDesc("Count");
        ApplySoftDelete(query);
        return (await qf.GetAsync<AuditActionCount>(query, null, null, ct)).ToList();
    }
}
