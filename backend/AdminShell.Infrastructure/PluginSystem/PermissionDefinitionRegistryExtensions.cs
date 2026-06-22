using AdminShell.Contracts;
using Dapper;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.PluginSystem;

public static class PermissionDefinitionRegistryExtensions
{
    public static async Task EnsurePermissionDefinitionsAsync(
        this IPermissionDefinitionRegistry registry,
        System.Data.IDbConnection connection,
        CancellationToken ct = default)
    {
        foreach (var definition in registry.GetAll())
        {
            ct.ThrowIfCancellationRequested();

            var qf = new QueryFactory(connection, new SqlServerCompiler());
            var exists = await qf.Query("Permissions")
                .Where("Code", definition.Code)
                .Where("IsDeleted", 0)
                .AsCount()
                .FirstAsync<int>();

            if (exists > 0)
                continue;

            await connection.ExecuteAsync(
                @"IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Code = @Code AND IsDeleted = 0)
                  BEGIN
                      INSERT INTO Permissions (Id, Code, Resource, Action, Description, IsDeleted, CreatedAt, CreatedBy)
                      VALUES (@Id, @Code, @Resource, @Action, @Description, 0, @CreatedAt, @CreatedBy);
                  END

                  DECLARE @AdminRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = N'Admin');
                  DECLARE @PermissionId UNIQUEIDENTIFIER = (SELECT Id FROM Permissions WHERE Code = @Code AND IsDeleted = 0);

                  IF @AdminRoleId IS NOT NULL AND @PermissionId IS NOT NULL
                  BEGIN
                      INSERT INTO RolePermissions (RoleId, PermissionId)
                      SELECT @AdminRoleId, @PermissionId
                      WHERE NOT EXISTS (
                          SELECT 1 FROM RolePermissions
                          WHERE RoleId = @AdminRoleId AND PermissionId = @PermissionId
                      );
                  END",
                new
                {
                    Id = Guid.NewGuid(),
                    Code = definition.Code,
                    Resource = definition.Group,
                    Action = definition.Name,
                    Description = $"Plugin permission: {definition.PluginId}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = definition.PluginId
                });
        }
    }
}
