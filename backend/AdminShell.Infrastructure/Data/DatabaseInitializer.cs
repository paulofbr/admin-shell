using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Infrastructure.Data.Migrations;
using AdminShell.Infrastructure.PluginSystem;
using DbUp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
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

        var qf = new QueryFactory(masterDb, new SqlServerCompiler());
        var countQuery = new Query("sys.databases")
            .Where("name", dbName)
            .AsCount();
        var exists = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(countQuery, null, null, default) switch
        {
            { } d when d.Values.FirstOrDefault() is int i => i,
            { } d when d.Values.FirstOrDefault() is long l => (int)l,
            _ => 0
        };

        if (exists == 0)
        {
            using var cmd = masterDb.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{dbName}]";
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Created database {DbName}", dbName);
        }
    }

    private async Task SeedDataAsync(IDbConnection db)
    {
        var qf = new QueryFactory(db, new SqlServerCompiler());

        var roleQuery = new Query("Roles")
            .Where("Name", "Admin")
            .Select("Id");
        var adminRoleId = await qf.FirstOrDefaultAsync<Guid?>(roleQuery, null, null, default);

        var now = DateTime.UtcNow;

        if (!adminRoleId.HasValue)
        {
            var roleAdminId = Guid.NewGuid();
            var roleUserId = Guid.NewGuid();

            var roleInsertQuery = new Query("Roles").AsInsert(new[]
            {
                new { Id = roleAdminId, Name = "Admin", Description = (string?)"Full system access", IsDeleted = 0, CreatedAt = now, CreatedBy = "system", UpdatedAt = now, UpdatedBy = "system" },
                new { Id = roleUserId, Name = "User", Description = (string?)"Standard user access", IsDeleted = 0, CreatedAt = now, CreatedBy = "system", UpdatedAt = now, UpdatedBy = "system" }
            });
            await qf.ExecuteAsync(roleInsertQuery, null, null, default);

            adminRoleId = roleAdminId;
            _logger.LogInformation("Admin role seeded");
        }

        var userQuery = new Query("Users")
            .Where("Email", "admin@admin.com")
            .Where("IsDeleted", 0)
            .Select("Id");
        var adminUserId = await qf.FirstOrDefaultAsync<Guid?>(userQuery, null, null, default);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

        if (!adminUserId.HasValue)
        {
            adminUserId = Guid.NewGuid();

            var userInsertQuery = new Query("Users").AsInsert(new
            {
                Id = adminUserId,
                Email = "admin@admin.com",
                Username = "admin",
                DisplayName = (string?)"Administrator",
                PasswordHash = passwordHash,
                IsActive = 1,
                IsDeleted = 0,
                CreatedAt = now,
                CreatedBy = "system",
                UpdatedAt = now,
                UpdatedBy = "system"
            });
            await qf.ExecuteAsync(userInsertQuery, null, null, default);

            _logger.LogInformation("Admin user seeded: admin@admin.com / admin123");
        }
        else
        {
            var userUpdateQuery = new Query("Users")
                .Where("Id", adminUserId)
                .AsUpdate(new
                {
                    PasswordHash = passwordHash,
                    DisplayName = "Administrator",
                    IsActive = 1,
                    UpdatedAt = now,
                    UpdatedBy = "system"
                });
            await qf.ExecuteAsync(userUpdateQuery, null, null, default);
        }

        var roleCountQuery = new Query("UserRoles AS ur")
            .Join("Roles AS r", "r.Id", "ur.RoleId")
            .Where("ur.UserId", adminUserId)
            .Where("r.Name", "Admin")
            .Where("r.IsDeleted", 0)
            .AsCount();
        var hasAdminRole = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(roleCountQuery, null, null, default) switch
        {
            { } d when d.Values.FirstOrDefault() is int i => i,
            { } d when d.Values.FirstOrDefault() is long l => (int)l,
            _ => 0
        };

        if (hasAdminRole == 0)
        {
            var userRoleInsertQuery = new Query("UserRoles").AsInsert(new
            {
                UserId = adminUserId,
                RoleId = adminRoleId
            });
            await qf.ExecuteAsync(userRoleInsertQuery, null, null, default);

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
            var permQuery = new Query("Permissions")
                .Where("Code", permission.Code)
                .Where("IsDeleted", 0)
                .Select("Id");
            var permissionId = await qf.FirstOrDefaultAsync<Guid?>(permQuery, null, null, default);

            if (!permissionId.HasValue)
            {
                permissionId = Guid.NewGuid();

                var permInsertQuery = new Query("Permissions").AsInsert(new
                {
                    Id = permissionId.Value,
                    Code = permission.Code,
                    Description = permission.Description,
                    Resource = permission.Resource,
                    Action = permission.Action,
                    IsDeleted = 0,
                    CreatedAt = now,
                    CreatedBy = "system",
                    UpdatedAt = now,
                    UpdatedBy = "system"
                });
                await qf.ExecuteAsync(permInsertQuery, null, null, default);
            }

            var rpInsertQuery = new Query("RolePermissions").AsInsert(new
            {
                RoleId = adminRoleId,
                PermissionId = permissionId
            });
            await qf.ExecuteAsync(rpInsertQuery, null, null, default);
        }

        var settingsCountQuery = new Query("Settings")
            .Where("IsDeleted", 0)
            .AsCount();
        var hasSettings = await qf.FirstOrDefaultAsync<IDictionary<string, object?>>(settingsCountQuery, null, null, default) switch
        {
            { } d when d.Values.FirstOrDefault() is int i => i,
            { } d when d.Values.FirstOrDefault() is long l => (int)l,
            _ => 0
        };

        if (hasSettings == 0)
        {
            var defaultSettings = new[]
            {
                new { Id = Guid.NewGuid(), Key = "site.name", Value = "Admin Shell", Category = "general", Description = (string?)"Application display name", ValueType = "string" },
                new { Id = Guid.NewGuid(), Key = "site.description", Value = "Admin management panel", Category = "general", Description = (string?)"Application description", ValueType = "string" },
                new { Id = Guid.NewGuid(), Key = "site.registration", Value = "true", Category = "general", Description = (string?)"Enable public registration", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "site.maintenance", Value = "false", Category = "general", Description = (string?)"Maintenance mode", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "security.session.timeout", Value = "60", Category = "security", Description = (string?)"Session timeout in minutes", ValueType = "number" },
                new { Id = Guid.NewGuid(), Key = "security.password.policy", Value = "medium", Category = "security", Description = (string?)"Password policy (low/medium/high)", ValueType = "string" },
                new { Id = Guid.NewGuid(), Key = "security.2fa", Value = "false", Category = "security", Description = (string?)"Require two-factor authentication", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "security.rate.limit", Value = "true", Category = "security", Description = (string?)"Rate limit login attempts", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "notifications.admin.email", Value = "admin@example.com", Category = "notifications", Description = (string?)"Admin notification email", ValueType = "string" },
                new { Id = Guid.NewGuid(), Key = "notifications.email.on.register", Value = "true", Category = "notifications", Description = (string?)"Email on new registration", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "notifications.email.on.plugin.error", Value = "true", Category = "notifications", Description = (string?)"Email on plugin errors", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "notifications.email.on.health", Value = "true", Category = "notifications", Description = (string?)"Email on health changes", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "plugins.directory", Value = "../plugins", Category = "plugins", Description = (string?)"Plugin directory path", ValueType = "string" },
                new { Id = Guid.NewGuid(), Key = "plugins.auto.discover", Value = "true", Category = "plugins", Description = (string?)"Auto-discover new plugins", ValueType = "boolean" },
                new { Id = Guid.NewGuid(), Key = "plugins.hot.reload", Value = "true", Category = "plugins", Description = (string?)"Enable hot-reload", ValueType = "boolean" },
            };

            var settingsInsertQuery = new Query("Settings").AsInsert(
                defaultSettings.Select(s => new { s.Id, s.Key, s.Value, s.Category, s.Description, s.ValueType, IsDeleted = 0, CreatedAt = now, CreatedBy = "system" }).ToList());
            await qf.ExecuteAsync(settingsInsertQuery, null, null, default);

            _logger.LogInformation("Default settings seeded");
        }
    }
}
