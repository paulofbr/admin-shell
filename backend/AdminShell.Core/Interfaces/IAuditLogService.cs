using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, string? entityId, string performedBy,
        string? previousValue = null, string? newValue = null, string? details = null,
        string? ipAddress = null, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 50, int skip = 0, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action, int count = 50, int skip = 0, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(string userId, int count = 50, int skip = 0, CancellationToken ct = default);
}