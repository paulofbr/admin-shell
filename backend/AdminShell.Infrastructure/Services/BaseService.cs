using AdminShell.Contracts;
using AdminShell.Core.Interfaces;

namespace AdminShell.Infrastructure.Services;

/// <summary>
/// Generic base service that can be extended by entity-specific services.
/// Provides common CRUD operations using result types.
/// </summary>
public abstract class BaseService<T, TDto, TCreate, TUpdate> : IBaseService<TDto, TCreate, TUpdate>
    where T : class
    where TDto : class
    where TCreate : class
    where TUpdate : class
{
    protected readonly IBaseRepository<T> Repository;
    protected readonly IAuditLogService? AuditLog;

    protected BaseService(IBaseRepository<T> repository, IAuditLogService? auditLog = null)
    {
        Repository = repository;
        AuditLog = auditLog;
    }

    public abstract Task<PagedResult<TDto>> GetAllAsync(QuerySpecification query, string? currentUser, CancellationToken ct = default);
    
    public abstract Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    
    public abstract Task<Result<TDto>> CreateAsync(TCreate request, string? currentUser, CancellationToken ct = default);
    
    public abstract Task<Result<TDto>> UpdateAsync(Guid id, TUpdate request, string? currentUser, CancellationToken ct = default);
    
    public abstract Task<Result> DeleteAsync(Guid id, string? currentUser, CancellationToken ct = default);

    protected async Task LogAsync(string action, string entityType, string entityId, string? user, string? details = null, string? previousValue = null, string? newValue = null, CancellationToken ct = default)
    {
        if (AuditLog != null)
        {
            await AuditLog.LogAsync(action, entityType, entityId, user ?? "system", previousValue: previousValue, newValue: newValue, details: details, ct: ct);
        }
    }
}