using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository repository, ILogger<AuditLogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, string? entityId,
        string performedBy, string? previousValue = null, string? newValue = null,
        string? details = null, string? ipAddress = null, CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                PreviousValue = previousValue,
                NewValue = newValue,
                PerformedBy = performedBy,
                IpAddress = ipAddress,
                Details = details,
            };

            await _repository.AddAsync(log, ct);
        }
        catch (Exception ex)
        {
            // Logging should never crash the application
            _logger.LogWarning(ex, "Failed to write audit log for {Action} on {Entity}", action, entityType);
        }
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 50, int skip = 0, CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(skip, count, ct);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        return await _repository.GetCountAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByActionAsync(string action,
        int count = 50, int skip = 0, CancellationToken ct = default)
    {
        return await _repository.GetByActionAsync(action, skip, count, ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(string userId,
        int count = 50, int skip = 0, CancellationToken ct = default)
    {
        return await _repository.GetByUserAsync(userId, skip, count, ct);
    }
}