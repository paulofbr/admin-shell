using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Dapper;

namespace AdminShell.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private const string EntityName = "User";

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPluginExtensionRegistry _extensionRegistry;

    public UserRepository(IDbConnectionFactory connectionFactory, IPluginExtensionRegistry extensionRegistry)
    {
        _connectionFactory = connectionFactory;
        _extensionRegistry = extensionRegistry;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
        if (user is not null)
        {
            user.Roles = await GetUserRolesAsync(db, id, ct);
            await HydrateExtensionFieldsAsync(db, user, ct);
        }
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email AND IsDeleted = 0",
            new { Email = email });
        if (user is not null)
        {
            user.Roles = await GetUserRolesAsync(db, user.Id, ct);
            await HydrateExtensionFieldsAsync(db, user, ct);
        }
        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var user = await db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsDeleted = 0",
            new { Username = username });
        if (user is not null)
        {
            user.Roles = await GetUserRolesAsync(db, user.Id, ct);
            await HydrateExtensionFieldsAsync(db, user, ct);
        }
        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(int skip = 0, int take = 20, string? email = null, string? username = null, string? displayName = null, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

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
            await HydrateExtensionFieldsAsync(db, user, ct);
        }
        return users;
    }

    public async Task<int> GetCountAsync(string? email = null, string? username = null, string? displayName = null, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();

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
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE IsDeleted = 0 AND IsActive = 1");
    }

    public async Task<List<MonthlyGrowthPoint>> GetMonthlyGrowthAsync(int months = 6, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
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

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var definitions = GetDefinitions();
        var parameters = new DynamicParameters();
        var columns = new List<string>
        {
            "Id", "Email", "Username", "DisplayName", "PasswordHash", "AvatarUrl", "IsActive", "IsDeleted", "CreatedAt", "CreatedBy"
        };
        var values = new List<string>
        {
            "@Id", "@Email", "@Username", "@DisplayName", "@PasswordHash", "@AvatarUrl", "@IsActive", "0", "@CreatedAt", "@CreatedBy"
        };

        parameters.Add("Id", user.Id);
        parameters.Add("Email", user.Email);
        parameters.Add("Username", user.Username);
        parameters.Add("DisplayName", user.DisplayName);
        parameters.Add("PasswordHash", user.PasswordHash);
        parameters.Add("AvatarUrl", user.AvatarUrl);
        parameters.Add("IsActive", user.IsActive);
        parameters.Add("CreatedAt", user.CreatedAt);
        parameters.Add("CreatedBy", user.CreatedBy);

        AddExtensionFieldsToParameters(parameters, definitions, user.ExtensionFields, columns, values);

        await db.ExecuteAsync(
            $"INSERT INTO Users ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})",
            parameters);
        await HydrateExtensionFieldsAsync(db, user, ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        var definitions = GetDefinitions();
        var parameters = new DynamicParameters();
        var assignments = new List<string>
        {
            "Email = @Email",
            "Username = @Username",
            "DisplayName = @DisplayName",
            "AvatarUrl = @AvatarUrl",
            "IsActive = @IsActive",
            "RefreshToken = @RefreshToken",
            "RefreshTokenExpiresAt = @RefreshTokenExpiresAt",
            "UpdatedAt = @UpdatedAt",
            "UpdatedBy = @UpdatedBy"
        };

        parameters.Add("Id", user.Id);
        parameters.Add("Email", user.Email);
        parameters.Add("Username", user.Username);
        parameters.Add("DisplayName", user.DisplayName);
        parameters.Add("AvatarUrl", user.AvatarUrl);
        parameters.Add("IsActive", user.IsActive);
        parameters.Add("RefreshToken", user.RefreshToken);
        parameters.Add("RefreshTokenExpiresAt", user.RefreshTokenExpiresAt);
        parameters.Add("UpdatedAt", user.UpdatedAt);
        parameters.Add("UpdatedBy", user.UpdatedBy);

        AddExtensionFieldsToAssignments(parameters, definitions, user.ExtensionFields, assignments);

        await db.ExecuteAsync(
            $"UPDATE Users SET {string.Join(", ", assignments)} WHERE Id = @Id",
            parameters);
        await HydrateExtensionFieldsAsync(db, user, ct);
    }

    public async Task DeleteAsync(User user, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        await db.ExecuteAsync(
            "UPDATE Users SET IsDeleted = 1, DeletedAt = @DeletedAt WHERE Id = @Id",
            new { user.Id, DeletedAt = DateTime.UtcNow });
    }

    private async Task HydrateExtensionFieldsAsync(System.Data.IDbConnection db, User user, CancellationToken ct)
    {
        var definitions = GetDefinitions();
        if (definitions.Count == 0)
            return;

        var row = await db.QueryFirstOrDefaultAsync(
            $"SELECT {string.Join(", ", definitions.Select(d => d.QuotedColumnName))} FROM Users WHERE Id = @Id",
            new { Id = user.Id });

        if (row is null)
            return;

        var values = row as IDictionary<string, object?>;
        if (values is null)
            return;

        user.ExtensionFields = definitions.Select(definition =>
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
