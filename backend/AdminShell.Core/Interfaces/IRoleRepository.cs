using AdminShell.Core.Entities;

namespace AdminShell.Core.Interfaces;

public interface IRoleRepository : IBaseRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
}
