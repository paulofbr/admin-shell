using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

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
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Id", id);
        ApplySoftDelete(query);
        var user = await qf.FirstOrDefaultAsync<User>(query, null, null, ct);
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
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Email", email);
        ApplySoftDelete(query);
        var user = await qf.FirstOrDefaultAsync<User>(query, null, null, ct);
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
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Username", username);
        ApplySoftDelete(query);
        var user = await qf.FirstOrDefaultAsync<User>(query, null, null, ct);
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
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).OrderBy("CreatedAt").Skip(skip).Take(take);
        ApplySoftDelete(query);

        if (!string.IsNullOrEmpty(email))
            query.WhereLike("Email", $"%{email}%");
        if (!string.IsNullOrEmpty(username))
            query.WhereLike("Username", $"%{username}%");
        if (!string.IsNullOrEmpty(displayName))
            query.WhereLike("DisplayName", $"%{displayName}%");

        var users = (await qf.GetAsync<User>(query, null, null, ct)).ToList();
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
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).AsCount();
        ApplySoftDelete(query);

        if (!string.IsNullOrEmpty(email))
            query.WhereLike("Email", $"%{email}%");
        if (!string.IsNullOrEmpty(username))
            query.WhereLike("Username", $"%{username}%");
        if (!string.IsNullOrEmpty(displayName))
            query.WhereLike("DisplayName", $"%{displayName}%");

        var result = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        return Convert.ToInt32(result?.Values.FirstOrDefault() ?? 0);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).AsCount().Where("IsDeleted", 0).Where("IsActive", 1);
        var result = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        return Convert.ToInt32(result?.Values.FirstOrDefault() ?? 0);
    }

    public async Task<List<MonthlyGrowthPoint>> GetMonthlyGrowthAsync(int months = 6, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName)
            .SelectRaw("FORMAT(CreatedAt, 'yyyy-MM') AS Month")
            .SelectRaw("COUNT(*) AS Count")
            .WhereRaw("CreatedAt >= DATEADD(MONTH, ?, GETUTCDATE())", months)
            .GroupByRaw("FORMAT(CreatedAt, 'yyyy-MM')")
            .OrderBy("Month");
        ApplySoftDelete(query);
        var rows = await qf.GetAsync<MonthlyGrowthPoint>(query, null, null, ct);
        return rows.ToList();
    }

    private static async Task<List<Role>> GetUserRolesAsync(System.Data.IDbConnection db, Guid userId, CancellationToken ct)
    {
        var qf = new QueryFactory(db, new SqlServerCompiler());
        var query = new Query("Roles AS r")
            .Select("r.Id", "r.Name", "r.Description", "r.IsDeleted", "r.DeletedAt", "r.CreatedAt", "r.CreatedBy")
            .Join("UserRoles", "r.Id", "UserRoles.RoleId")
            .Where("UserRoles.UserId", userId);
        var roles = (await qf.GetAsync<Role>(query, null, null, ct)).ToList();
        foreach (var role in roles)
        {
            role.Permissions = await GetRolePermissionsAsync(db, role.Id, ct);
        }
        return roles;
    }

    private static async Task<List<Permission>> GetRolePermissionsAsync(System.Data.IDbConnection db, Guid roleId, CancellationToken ct)
    {
        var qf = new QueryFactory(db, new SqlServerCompiler());
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
}
