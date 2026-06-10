using System.Linq.Expressions;

namespace AdminShell.Core.Interfaces;

/// <summary>
/// Base repository with standard CRUD operations.
/// Entities use GUID for Id, soft-delete with IsDeleted, audit fields (CreatedAt/By, UpdatedAt/By).
/// </summary>
public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> GetByIdAsync(Guid id, Expression<Func<T, object>> includes, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(int skip = 0, int take = 20, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default); // Soft delete
    Task HardDeleteAsync(T entity, CancellationToken ct = default); // Physical delete
}

/// <summary>
/// Base service with standard CRUD operations.
/// Returns Result<T> for consistent error handling.
/// </summary>
public interface IBaseService<TDto, TCreateRequest, TUpdateRequest>
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    Task<PagedResult<TDto>> GetAllAsync(int skip, int take, string? currentUser, CancellationToken ct = default);
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TDto>> CreateAsync(TCreateRequest request, string? currentUser, CancellationToken ct = default);
    Task<Result<TDto>> UpdateAsync(Guid id, TUpdateRequest request, string? currentUser, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, string? currentUser, CancellationToken ct = default);
}