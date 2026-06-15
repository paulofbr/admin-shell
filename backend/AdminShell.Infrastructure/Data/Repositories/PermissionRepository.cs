using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var perms = await db.QueryAsync<Permission>(
            "SELECT * FROM Permissions WHERE IsDeleted = 0 ORDER BY Resource, Action");
        return perms.ToList();
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.QueryFirstOrDefaultAsync<Permission>(
            "SELECT * FROM Permissions WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.QueryFirstOrDefaultAsync<Permission>(
            "SELECT * FROM Permissions WHERE Code = @Code AND IsDeleted = 0",
            new { Code = code });
    }

    public async Task<IReadOnlyList<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var perms = await db.QueryAsync<Permission>(
            @"SELECT p.Id, p.Code, p.Resource, p.Action, p.Description,
                     p.IsDeleted, p.DeletedAt, p.CreatedAt, p.CreatedBy
              FROM Permissions p
              INNER JOIN RolePermissions ON p.Id = RolePermissions.PermissionId
              WHERE RolePermissions.RoleId = @RoleId AND p.IsDeleted = 0
              ORDER BY p.Resource, p.Action",
            new { RoleId = roleId });
        return perms.ToList();
    }

    public async Task AssignToRoleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (@RoleId, @PermissionId)",
            new { RoleId = roleId, PermissionId = permissionId });
    }

    public async Task RemoveFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "DELETE FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId",
            new { RoleId = roleId, PermissionId = permissionId });
    }
}