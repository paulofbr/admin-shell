using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
        : base(connectionFactory, extensionRegistry)
    {
    }

    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
        if (user is not null)
        {
            user.Roles = await GetUserRolesAsync(db, id, ct);
            await AfterLoadAsync(db, user, ct);
        }
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email AND IsDeleted = 0",
            new { Email = email });
        if (user is not null)
        {
            user.Roles = await GetUserRolesAsync(db, user.Id, ct);
            await AfterLoadAsync(db, user, ct);
        }
        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsDeleted = 0",
            new { Username = username });
        if (user is not null)
        {
            user.Roles = await GetUserRolesAsync(db, user.Id, ct);
            await AfterLoadAsync(db, user, ct);
        }
        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(int skip = 0, int take = 20, string? email = null, string? username = null, string? displayName = null, CancellationToken ct = default)
    {
        using var db = CreateConnection();

        var sql = @"SELECT * FROM Users WHERE IsDeleted = 0";
        var parameters = new DynamicParameters();
        parameters.Add("@Skip", skip);
        parameters.Add("@Take", take);

        if (!string.IsNullOrEmpty(email))
        {
            sql += " AND Email LIKE @Email";
            parameters.Add("@Email", $"%{email}%");
        }
        if (!string.IsNullOrEmpty(username))
        {
            sql += " AND Username LIKE @Username";
            parameters.Add("@Username", $"%{username}%");
        }
        if (!string.IsNullOrEmpty(displayName))
        {
            sql += " AND DisplayName LIKE @DisplayName";
            parameters.Add("@DisplayName", $"%{displayName}%");
        }

        sql += " ORDER BY CreatedAt ASC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var users = (await db.QueryAsync<User>(sql, parameters)).ToList();
        foreach (var user in users)
        {
            user.Roles = await GetUserRolesAsync(db, user.Id, ct);
            await AfterLoadAsync(db, user, ct);
        }
        return users;
    }

    public async Task<int> GetCountAsync(string? email = null, string? username = null, string? displayName = null, CancellationToken ct = default)
    {
        using var db = CreateConnection();

        var sql = "SELECT COUNT(*) FROM Users WHERE IsDeleted = 0";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(email))
        {
            sql += " AND Email LIKE @Email";
            parameters.Add("@Email", $"%{email}%");
        }
        if (!string.IsNullOrEmpty(username))
        {
            sql += " AND Username LIKE @Username";
            parameters.Add("@Username", $"%{username}%");
        }
        if (!string.IsNullOrEmpty(displayName))
        {
            sql += " AND DisplayName LIKE @DisplayName";
            parameters.Add("@DisplayName", $"%{displayName}%");
        }

        return await db.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE IsDeleted = 0 AND IsActive = 1");
    }

    public async Task<List<MonthlyGrowthPoint>> GetMonthlyGrowthAsync(int months = 6, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var rows = await db.QueryAsync<MonthlyGrowthPoint>(
            @"SELECT
                FORMAT(CreatedAt, 'yyyy-MM') AS Month,
                COUNT(*) AS Count
              FROM Users
              WHERE IsDeleted = 0 AND CreatedAt >= DATEADD(MONTH, -@Months, GETUTCDATE())
              GROUP BY FORMAT(CreatedAt, 'yyyy-MM')
              ORDER BY Month",
            new { Months = months });
        return rows.ToList();
    }

    private static async Task<List<Role>> GetUserRolesAsync(System.Data.IDbConnection db, Guid userId, CancellationToken ct)
    {
        var roles = await db.QueryAsync<Role>(
            @"SELECT r.Id, r.Name, r.Description, r.IsDeleted, r.DeletedAt, r.CreatedAt, r.CreatedBy
              FROM Roles r
              INNER JOIN UserRoles ON r.Id = UserRoles.RoleId
              WHERE UserRoles.UserId = @UserID",
            new { UserID = userId });
        var roleList = roles.ToList();
        foreach (var role in roleList)
        {
            role.Permissions = await GetRolePermissionsAsync(db, role.Id, ct);
        }
        return roleList;
    }

    private static async Task<List<Permission>> GetRolePermissionsAsync(System.Data.IDbConnection db, Guid roleId, CancellationToken ct)
    {
        var permissions = await db.QueryAsync<Permission>(
            @"SELECT p.Id, p.Code, p.Resource, p.Action, p.Description,
                     p.IsDeleted, p.DeletedAt, p.CreatedAt, p.CreatedBy
              FROM Permissions p
              INNER JOIN RolePermissions ON p.Id = RolePermissions.PermissionId
              WHERE RolePermissions.RoleId = @RoleId AND p.IsDeleted = 0
              ORDER BY p.Resource, p.Action",
            new { RoleId = roleId });
        return permissions.ToList();
    }
}
