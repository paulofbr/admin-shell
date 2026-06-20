using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Infrastructure.Data.Migrations;
using AdminShell.Infrastructure.PluginSystem;
using Dapper;
using DbUp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AdminShell.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPluginExtensionRegistry _extensionRegistry;
    private readonly IManagedEntitySchemaManager _managedEntitySchemaManager;
    private readonly IPermissionDefinitionRegistry _permissionDefinitionRegistry;
    private readonly ISettingsRegistry _settingsRegistry;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbConnectionFactory connectionFactory,
        IPluginExtensionRegistry extensionRegistry,
        IManagedEntitySchemaManager managedEntitySchemaManager,
        IPermissionDefinitionRegistry permissionDefinitionRegistry,
        ISettingsRegistry settingsRegistry,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _extensionRegistry = extensionRegistry;
        _managedEntitySchemaManager = managedEntitySchemaManager;
        _permissionDefinitionRegistry = permissionDefinitionRegistry;
        _settingsRegistry = settingsRegistry;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseExistsAsync();

        var connectionString = _connectionFactory.CreateConnection().ConnectionString;

        // Run DbUp embedded SQL migrations
        var result = MigrationRunner.RunMigrations(connectionString, _logger);
        if (!result.Successful)
            throw new InvalidOperationException($"Database migration failed: {result.Error}");

        using var db = _connectionFactory.CreateConnection();
        db.Open();

        await _managedEntitySchemaManager.EnsureAsync(db);
        await _extensionRegistry.ApplyAllMigrationsAsync(db);
        await SeedDataAsync(db);
        await _permissionDefinitionRegistry.EnsurePermissionDefinitionsAsync(db);
        await _settingsRegistry.EnsureDefaultsAsync();

        _logger.LogInformation("Database initialized successfully");
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var masterCs = _connectionFactory.CreateConnection().ConnectionString;
        var builder = new SqlConnectionStringBuilder(masterCs);
        builder.InitialCatalog = "master";

        using var masterDb = new SqlConnection(builder.ConnectionString);
        masterDb.Open();

        var dbName = builder.InitialCatalog = "AdminShell";
        var dbNameFromCs = new SqlConnectionStringBuilder(_connectionFactory.CreateConnection().ConnectionString).InitialCatalog;
        dbName = string.IsNullOrWhiteSpace(dbNameFromCs) ? "AdminShell" : dbNameFromCs;

        var exists = await masterDb.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys.databases WHERE name = @Name",
            new { Name = dbName });

        if (exists == 0)
        {
            await masterDb.ExecuteAsync($"CREATE DATABASE [{dbName}]");
            _logger.LogInformation("Created database {DbName}", dbName);
        }
    }

    private async Task SeedDataAsync(IDbConnection db)
    {
        var adminRoleId = await db.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT TOP 1 Id FROM Roles WHERE Name = @Name",
            new { Name = "Admin" });

        var now = DateTime.UtcNow;

        if (!adminRoleId.HasValue)
        {
            var roleAdminId = Guid.NewGuid();
            var roleUserId = Guid.NewGuid();

            await db.ExecuteAsync(
                @"INSERT INTO Roles (Id, Name, Description, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                  VALUES (@Id, @Name, @Desc, 0, @Now, 'system', @Now, 'system')",
                new[] {
                    new { Id = roleAdminId, Name = "Admin", Desc = (string?)"Full system access", Now = now },
                    new { Id = roleUserId, Name = "User", Desc = (string?)"Standard user access", Now = now }
                });

            adminRoleId = roleAdminId;
            _logger.LogInformation("Admin role seeded");
        }

        var adminUserId = await db.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT TOP 1 Id FROM Users WHERE Email = @Email AND IsDeleted = 0",
            new { Email = "admin@admin.com" });

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

        if (!adminUserId.HasValue)
        {
            adminUserId = Guid.NewGuid();

            await db.ExecuteAsync(
                @"INSERT INTO Users (Id, Email, Username, DisplayName, PasswordHash, IsActive, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                  VALUES (@Id, @Email, @Username, @DisplayName, @Pwd, 1, 0, @Now, 'system', @Now, 'system')",
                new { Id = adminUserId, Email = "admin@admin.com", Username = "admin", DisplayName = (string?)"Administrator", Pwd = passwordHash, Now = now });

            _logger.LogInformation("Admin user seeded: admin@admin.com / admin123");
        }
        else
        {
            await db.ExecuteAsync(
                @"UPDATE Users
                  SET PasswordHash = @Pwd,
                      DisplayName = COALESCE(NULLIF(DisplayName, ''), @DisplayName),
                      IsActive = 1,
                      UpdatedAt = @Now,
                      UpdatedBy = 'system'
                  WHERE Id = @Id",
                new { Id = adminUserId, Pwd = passwordHash, DisplayName = (string?)"Administrator", Now = now });
        }

        var hasAdminRole = await db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1)
              FROM UserRoles ur
              INNER JOIN Roles r ON r.Id = ur.RoleId
              WHERE ur.UserId = @UserId AND r.Name = 'Admin' AND r.IsDeleted = 0",
            new { UserId = adminUserId });

        if (hasAdminRole == 0)
        {
            await db.ExecuteAsync(
                @"INSERT INTO UserRoles (UserId, RoleId)
                  SELECT @UserId, @RoleId
                  WHERE NOT EXISTS (
                      SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)",
                new { UserId = adminUserId, RoleId = adminRoleId });

            _logger.LogInformation("Admin role assigned to admin@admin.com");
        }

        var permissions = new[]
        {
            new { Code = "users:read", Resource = "users", Action = "read", Description = (string?)"Read users" },
            new { Code = "users:write", Resource = "users", Action = "write", Description = (string?)"Write users" },
            new { Code = "users:delete", Resource = "users", Action = "delete", Description = (string?)"Delete users" },
            new { Code = "plugins:manage", Resource = "plugins", Action = "manage", Description = (string?)"Manage plugins" },
            new { Code = "settings:read", Resource = "settings", Action = "read", Description = (string?)"Read settings" },
            new { Code = "settings:write", Resource = "settings", Action = "write", Description = (string?)"Write settings" },
        };

        foreach (var permission in permissions)
        {
            var permissionId = await db.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT TOP 1 Id FROM Permissions WHERE Code = @Code AND IsDeleted = 0",
                permission);

            if (!permissionId.HasValue)
            {
                permissionId = Guid.NewGuid();

                await db.ExecuteAsync(
                    @"INSERT INTO Permissions (Id, Code, Description, Resource, Action, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                      VALUES (@Id, @Code, @Description, @Resource, @Action, 0, @Now, 'system', @Now, 'system')",
                    new { Id = permissionId.Value, permission.Code, permission.Description, permission.Resource, permission.Action, Now = now });
            }

            await db.ExecuteAsync(
                @"INSERT INTO RolePermissions (RoleId, PermissionId)
                  SELECT @RoleId, @PermissionId
                  WHERE NOT EXISTS (
                      SELECT 1 FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId)",
                new { RoleId = adminRoleId, PermissionId = permissionId });
        }

        var hasSettings = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Settings WHERE IsDeleted = 0");
        if (hasSettings == 0)
        {
            var defaultSettings = new[]
            {
                new { Id = Guid.NewGuid(), Key = "site.name", Value = "Admin Shell", Category = "general", Desc = (string?)"Application display name", Type = "string" },
                new { Id = Guid.NewGuid(), Key = "site.description", Value = "Admin management panel", Category = "general", Desc = (string?)"Application description", Type = "string" },
                new { Id = Guid.NewGuid(), Key = "site.registration", Value = "true", Category = "general", Desc = (string?)"Enable public registration", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "site.maintenance", Value = "false", Category = "general", Desc = (string?)"Maintenance mode", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "security.session.timeout", Value = "60", Category = "security", Desc = (string?)"Session timeout in minutes", Type = "number" },
                new { Id = Guid.NewGuid(), Key = "security.password.policy", Value = "medium", Category = "security", Desc = (string?)"Password policy (low/medium/high)", Type = "string" },
                new { Id = Guid.NewGuid(), Key = "security.2fa", Value = "false", Category = "security", Desc = (string?)"Require two-factor authentication", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "security.rate.limit", Value = "true", Category = "security", Desc = (string?)"Rate limit login attempts", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "notifications.admin.email", Value = "admin@example.com", Category = "notifications", Desc = (string?)"Admin notification email", Type = "string" },
                new { Id = Guid.NewGuid(), Key = "notifications.email.on.register", Value = "true", Category = "notifications", Desc = (string?)"Email on new registration", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "notifications.email.on.plugin.error", Value = "true", Category = "notifications", Desc = (string?)"Email on plugin errors", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "notifications.email.on.health", Value = "true", Category = "notifications", Desc = (string?)"Email on health changes", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "plugins.directory", Value = "../plugins", Category = "plugins", Desc = (string?)"Plugin directory path", Type = "string" },
                new { Id = Guid.NewGuid(), Key = "plugins.auto.discover", Value = "true", Category = "plugins", Desc = (string?)"Auto-discover new plugins", Type = "boolean" },
                new { Id = Guid.NewGuid(), Key = "plugins.hot.reload", Value = "true", Category = "plugins", Desc = (string?)"Enable hot-reload", Type = "boolean" },
            };

            await db.ExecuteAsync(
                @"INSERT INTO Settings (Id, [Key], Value, Category, Description, ValueType, IsDeleted, CreatedAt, CreatedBy)
                  VALUES (@Id, @Key, @Value, @Category, @Desc, @Type, 0, @Now, 'system')",
                defaultSettings.Select(s => new { s.Id, s.Key, s.Value, s.Category, s.Desc, s.Type, Now = now }).ToList());

            _logger.LogInformation("Default settings seeded");
        }
    }
}
