using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using Dapper;
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

        var logs = await db.QueryAsync<AuditLog>(
            @"SELECT * FROM AuditLogs WHERE Action = @Action AND IsDeleted = 0
              ORDER BY CreatedAt DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { Action = action, Skip = skip, Take = take });

        return logs.ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = CreateConnection();

        var logs = await db.QueryAsync<AuditLog>(
            @"SELECT * FROM AuditLogs WHERE PerformedBy = @User AND IsDeleted = 0
              ORDER BY CreatedAt DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { User = userId, Skip = skip, Take = take });

        return logs.ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to,
        int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = CreateConnection();

        var logs = await db.QueryAsync<AuditLog>(
            @"SELECT * FROM AuditLogs
              WHERE CreatedAt >= @From AND CreatedAt <= @To AND IsDeleted = 0
              ORDER BY CreatedAt DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { From = from, To = to, Skip = skip, Take = take });

        return logs.ToList();
    }

    public async Task<int> GetCountSinceAsync(DateTime since, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditLogs WHERE IsDeleted = 0 AND CreatedAt >= @Since",
            new { Since = since });
    }

    public async Task<int> GetCountByActionSinceAsync(string action, DateTime since, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditLogs WHERE IsDeleted = 0 AND Action = @Action AND CreatedAt >= @Since",
            new { Action = action, Since = since });
    }

    public async Task<List<AuditActionCount>> GetCountByActionGroupAsync(DateTime since, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var rows = await db.QueryAsync<AuditActionCount>(
            @"SELECT Action, COUNT(*) AS Count
              FROM AuditLogs
              WHERE IsDeleted = 0 AND CreatedAt >= @Since
              GROUP BY Action
              ORDER BY Count DESC",
            new { Since = since });
        return rows.ToList();
    }
}