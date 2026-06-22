using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AdminShell.Infrastructure.Data.Repositories;

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry? extensionRegistry = null)
        : base(connectionFactory, extensionRegistry)
    {
    }

    public override async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Id", id);
        ApplySoftDelete(query);
        var role = await qf.FirstOrDefaultAsync<Role>(query, null, null, ct);
        if (role is not null)
        {
            role.Permissions = await GetRolePermissionsAsync(db, id, ct);
            await AfterLoadAsync(db, role, ct);
        }
        return role;
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).Where("Name", name);
        ApplySoftDelete(query);
        var role = await qf.FirstOrDefaultAsync<Role>(query, null, null, ct);
        if (role is not null)
        {
            role.Permissions = await GetRolePermissionsAsync(db, role.Id, ct);
            await AfterLoadAsync(db, role, ct);
        }
        return role;
    }

    public override async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).OrderBy("Name");
        ApplySoftDelete(query);
        var roles = (await qf.GetAsync<Role>(query, null, null, ct)).ToList();
        foreach (var role in roles)
        {
            role.Permissions = await GetRolePermissionsAsync(db, role.Id, ct);
            await AfterLoadAsync(db, role, ct);
        }
        return roles;
    }

    public override async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var query = new Query(TableName).AsCount();
        ApplySoftDelete(query);
        var result = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(query, null, null, ct);
        return Convert.ToInt32(result?.Values.FirstOrDefault() ?? 0);
    }

    public override async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var data = new Dictionary<string, object>
        {
            ["Id"] = role.Id,
            ["Name"] = role.Name,
            ["Description"] = (object?)role.Description ?? DBNull.Value,
            ["IsDeleted"] = 0,
            ["CreatedAt"] = role.CreatedAt,
            ["CreatedBy"] = (object?)role.CreatedBy ?? DBNull.Value
        };

        var definitions = GetDefinitions();
        foreach (var definition in definitions)
        {
            var field = role.ExtensionFields.FirstOrDefault(f =>
                string.Equals(f.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var value = field?.Value ?? definition.DefaultValue;
            data[definition.ColumnName] = NormalizeValue(value, definition.Type) ?? DBNull.Value;
        }

        await qf.ExecuteAsync(new Query(TableName).AsInsert(data), null, null, ct);
        await AfterLoadAsync(db, role, ct);
        return role;
    }

    public override async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        var data = new Dictionary<string, object>
        {
            ["Name"] = role.Name,
            ["Description"] = (object?)role.Description ?? DBNull.Value
        };

        var definitions = GetDefinitions();
        foreach (var definition in definitions)
        {
            var field = role.ExtensionFields.FirstOrDefault(f =>
                string.Equals(f.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var value = field?.Value ?? definition.DefaultValue;
            data[definition.ColumnName] = NormalizeValue(value, definition.Type) ?? DBNull.Value;
        }

        await qf.ExecuteAsync(new Query(TableName).Where("Id", role.Id).AsUpdate(data), null, null, ct);
        await AfterLoadAsync(db, role, ct);
    }

    public override async Task DeleteAsync(Role role, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var qf = CreateQueryFactory(db);
        await qf.ExecuteAsync(
            new Query(TableName).Where("Id", role.Id).AsUpdate(new Dictionary<string, object>
            {
                ["IsDeleted"] = 1,
                ["DeletedAt"] = DateTime.UtcNow
            }),
            null, null, ct);
    }

    public static async Task<List<Permission>> GetRolePermissionsAsync(System.Data.IDbConnection db, Guid roleId, CancellationToken ct)
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

    private static object? NormalizeValue(object? value, EntityExtensionFieldType type)
    {
        if (value is null)
            return null;

        return type switch
        {
            EntityExtensionFieldType.Boolean => value is bool boolean ? boolean : bool.TryParse(Convert.ToString(value), out var parsedBool) ? parsedBool : value,
            EntityExtensionFieldType.Number => value is decimal or double or int or long ? value : decimal.TryParse(Convert.ToString(value), out var parsedDecimal) ? parsedDecimal : value,
            _ => value
        };
    }
}
