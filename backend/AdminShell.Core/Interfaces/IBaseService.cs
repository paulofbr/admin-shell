using AdminShell.Contracts;

namespace AdminShell.Core.Interfaces;

public interface IBaseService<TDto, TCreate, TUpdate>
{
    Task<PagedResult<TDto>> GetAllAsync(QuerySpecification query, string? currentUser, CancellationToken ct = default);
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TDto>> CreateAsync(TCreate request, string? currentUser, CancellationToken ct = default);
    Task<Result<TDto>> UpdateAsync(Guid id, TUpdate request, string? currentUser, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, string? currentUser, CancellationToken ct = default);
}
