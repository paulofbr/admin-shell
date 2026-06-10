using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuditLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuditLog> AddAsync(AuditLog log, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        log.Id = Guid.NewGuid();
        log.CreatedAt = DateTime.UtcNow;

        await db.ExecuteAsync(
            @"INSERT INTO AuditLogs (Id, Action, EntityType, EntityId, PreviousValue, NewValue,
              PerformedBy, IpAddress, Details, IsDeleted, CreatedAt, CreatedBy)
              VALUES (@Id, @Action, @EntityType, @EntityId, @PreviousValue, @NewValue,
              @PerformedBy, @IpAddress, @Details, 0, @CreatedAt, @PerformedBy)",
            log);

        return log;
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        var logs = await db.QueryAsync<AuditLog>(
            @"SELECT * FROM AuditLogs WHERE IsDeleted = 0
              ORDER BY CreatedAt DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { Skip = skip, Take = take });

        return logs.ToList();
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditLogs WHERE IsDeleted = 0");
    }

    public async Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

        var logs = await db.QueryAsync<AuditLog>(
            @"SELECT * FROM AuditLogs WHERE Action = @Action AND IsDeleted = 0
              ORDER BY CreatedAt DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { Action = action, Skip = skip, Take = take });

        return logs.ToList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

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
        using var db = _connectionFactory.CreateConnection();
        db.Open();

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
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditLogs WHERE IsDeleted = 0 AND CreatedAt >= @Since",
            new { Since = since });
    }

    public async Task<int> GetCountByActionSinceAsync(string action, DateTime since, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditLogs WHERE IsDeleted = 0 AND Action = @Action AND CreatedAt >= @Since",
            new { Action = action, Since = since });
    }

    public async Task<List<AuditActionCount>> GetCountByActionGroupAsync(DateTime since, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
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