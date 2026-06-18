using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class RoleRepository : IRoleRepository
{
    private const string EntityName = "Role";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPluginExtensionRegistry _extensionRegistry;

    public RoleRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry extensionRegistry)
    {
        _connectionFactory = connectionFactory;
        _extensionRegistry = extensionRegistry;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var role = await db.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
        if (role is not null)
            await HydrateExtensionFieldsAsync(db, role, ct);
        return role;
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var role = await db.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Name = @Name AND IsDeleted = 0",
            new { Name = name });
        if (role is not null)
            await HydrateExtensionFieldsAsync(db, role, ct);
        return role;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var roles = (await db.QueryAsync<Role>(
            "SELECT * FROM Roles WHERE IsDeleted = 0 ORDER BY Name")).ToList();
        foreach (var role in roles)
            await HydrateExtensionFieldsAsync(db, role, ct);
        return roles;
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Roles WHERE IsDeleted = 0");
    }

    public async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
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
        await HydrateExtensionFieldsAsync(db, role, ct);
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
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
        await HydrateExtensionFieldsAsync(db, role, ct);
    }

    public async Task DeleteAsync(Role role, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "UPDATE Roles SET IsDeleted = 1, DeletedAt = @DeletedAt WHERE Id = @Id",
            new { role.Id, DeletedAt = DateTime.UtcNow });
    }

    private async Task HydrateExtensionFieldsAsync(System.Data.IDbConnection db, Role role, CancellationToken ct)
    {
        var definitions = GetDefinitions();
        if (definitions.Count == 0)
            return;

        var row = await db.QueryFirstOrDefaultAsync(
            $"SELECT {string.Join(", ", definitions.Select(d => d.QuotedColumnName))} FROM Roles WHERE Id = @Id",
            new { Id = role.Id });

        if (row is null)
            return;

        var values = row as IDictionary<string, object?>;
        if (values is null)
            return;

        role.ExtensionFields = definitions.Select(definition =>
        {
            var rawValue = values.TryGetValue(definition.ColumnName, out var value) ? value : null;
            return new ExtensionField
            {
                Name = definition.Name,
                Value = ConvertValue(rawValue, definition.Type),
                Type = definition.Type.ToString(),
                Required = definition.Required,
                DefaultValue = definition.DefaultValue,
                Label = definition.Label,
                PossibleValues = definition.PossibleValues,
                FrontEndValidator = definition.FrontEndValidator,
                Slot = definition.Slot
            };
        }).ToList();
    }

    private IReadOnlyList<EntityExtensionFieldDefinition> GetDefinitions()
        => _extensionRegistry.GetExtensionFieldsForEntity(EntityName).ToList();

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

    private static object? ConvertValue(object? value, EntityExtensionFieldType type)
    {
        if (value is null || value is DBNull)
            return null;

        return type switch
        {
            EntityExtensionFieldType.Boolean => value is bool boolean ? boolean : bool.TryParse(Convert.ToString(value), out var parsedBool) && parsedBool,
            EntityExtensionFieldType.Number => value is decimal decimalValue ? decimalValue : decimal.TryParse(Convert.ToString(value), out var parsedDecimal) ? parsedDecimal : value,
            _ => value
        };
    }
}
