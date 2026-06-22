using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.Data.Repositories;

public class PermissionRepository : RepositoryBase<Permission>, IPermissionRepository
{
    public PermissionRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
        : base(connectionFactory, extensionRegistry)
    {
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Code", code);
        ApplySoftDelete(query);
        return await qf.FirstOrDefaultAsync<Permission>(query, null, null, ct);
    }

    public async Task<IReadOnlyList<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query("Permissions AS p")
            .Select("p.Id", "p.Code", "p.Resource", "p.Action", "p.Description",
                     "p.IsDeleted", "p.DeletedAt", "p.CreatedAt", "p.CreatedBy")
            .Join("RolePermissions", "p.Id", "RolePermissions.PermissionId")
            .Where("RolePermissions.RoleId", roleId)
            .Where("p.IsDeleted", 0)
            .OrderBy("p.Resource")
            .OrderBy("p.Action");
        return (await qf.GetAsync<Permission>(query, null, null, ct)).ToList();
    }

    public async Task AssignToRoleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query("RolePermissions").AsInsert(new Dictionary<string, object>
        {
            ["RoleId"] = roleId,
            ["PermissionId"] = permissionId
        });
        await qf.ExecuteAsync(query, null, null, ct);
    }

    public async Task RemoveFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query("RolePermissions")
            .Where("RoleId", roleId)
            .Where("PermissionId", permissionId)
            .AsDelete();
        await qf.ExecuteAsync(query, null, null, ct);
    }
}
