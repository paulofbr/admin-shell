using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default);
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default);
    Task AssignToRoleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task RemoveFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
}