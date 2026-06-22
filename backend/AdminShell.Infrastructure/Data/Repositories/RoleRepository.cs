using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Data;
using Dapper;

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
        var role = await db.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
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
        var role = await db.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Name = @Name AND IsDeleted = 0",
            new { Name = name });
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
        var roles = (await db.QueryAsync<Role>(
            "SELECT * FROM Roles WHERE IsDeleted = 0 ORDER BY Name")).ToList();
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
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Roles WHERE IsDeleted = 0");
    }

    public override async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var definitions = GetDefinitions();
        var parameters = new DynamicParameters();
        var columns = new List<string> { "Id", "Name", "Description", "IsDeleted", "CreatedAt", "CreatedBy" };
        var values = new List<string> { "@Id", "@Name", "@Description", "0", "@CreatedAt", "@CreatedBy" };

        parameters.Add("Id", role.Id);
        parameters.Add("Name", role.Name);
        parameters.Add("Description", role.Description);
        parameters.Add("CreatedAt", role.CreatedAt);
        parameters.Add("CreatedBy", role.CreatedBy);

        AddExtensionFieldsToParameters(parameters, definitions, role.ExtensionFields, columns, values);

        await db.ExecuteAsync(
            $"INSERT INTO Roles ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})",
            parameters);
        await AfterLoadAsync(db, role, ct);
        return role;
    }

    public override async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        var definitions = GetDefinitions();
        var parameters = new DynamicParameters();
        var assignments = new List<string> { "Name = @Name", "Description = @Description" };

        parameters.Add("Id", role.Id);
        parameters.Add("Name", role.Name);
        parameters.Add("Description", role.Description);

        AddExtensionFieldsToAssignments(parameters, definitions, role.ExtensionFields, assignments);

        await db.ExecuteAsync(
            $"UPDATE Roles SET {string.Join(", ", assignments)} WHERE Id = @Id",
            parameters);
        await AfterLoadAsync(db, role, ct);
    }

    public override async Task DeleteAsync(Role role, CancellationToken ct = default)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            "UPDATE Roles SET IsDeleted = 1, DeletedAt = @DeletedAt WHERE Id = @Id",
            new { role.Id, DeletedAt = DateTime.UtcNow });
    }

    public static async Task<List<Permission>> GetRolePermissionsAsync(System.Data.IDbConnection db, Guid roleId, CancellationToken ct)
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

    private static void AddExtensionFieldsToParameters(
        DynamicParameters parameters,
        IReadOnlyList<EntityExtensionFieldDefinition> definitions,
        IReadOnlyList<ExtensionField> extensionFields,
        ICollection<string> columns,
        ICollection<string> values)
    {
        foreach (var definition in definitions)
        {
            var field = extensionFields.FirstOrDefault(f => string.Equals(f.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var value = field?.Value ?? definition.DefaultValue;
            var columnName = definition.ColumnName;
            columns.Add(definition.QuotedColumnName);
            values.Add($"@{columnName}");
            parameters.Add(columnName, NormalizeValue(value, definition.Type));
        }
    }

    private static void AddExtensionFieldsToAssignments(
        DynamicParameters parameters,
        IReadOnlyList<EntityExtensionFieldDefinition> definitions,
        IReadOnlyList<ExtensionField> extensionFields,
        ICollection<string> assignments)
    {
        foreach (var definition in definitions)
        {
            var field = extensionFields.FirstOrDefault(f => string.Equals(f.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            var value = field?.Value ?? definition.DefaultValue;
            var columnName = definition.ColumnName;
            assignments.Add($"{definition.QuotedColumnName} = @{columnName}");
            parameters.Add(columnName, NormalizeValue(value, definition.Type));
        }
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
