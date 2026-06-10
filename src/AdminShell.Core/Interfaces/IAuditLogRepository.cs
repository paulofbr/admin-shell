using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<int> GetCountSinceAsync(DateTime since, CancellationToken ct = default);
    Task<int> GetCountByActionSinceAsync(string action, DateTime since, CancellationToken ct = default);
    Task<List<AuditActionCount>> GetCountByActionGroupAsync(DateTime since, CancellationToken ct = default);
}