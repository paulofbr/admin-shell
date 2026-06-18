using AdminShell.Contracts;
using AdminShell.Core.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AdminShell.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPluginExtensionRegistry _extensionRegistry;
    private readonly IManagedEntitySchemaManager _managedEntitySchemaManager;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbConnectionFactory connectionFactory,
        IPluginExtensionRegistry extensionRegistry,
        IManagedEntitySchemaManager managedEntitySchemaManager,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _extensionRegistry = extensionRegistry;
        _managedEntitySchemaManager = managedEntitySchemaManager;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        // Ensure the database exists (connect to master first)
        await EnsureDatabaseExistsAsync();

        using var db = _connectionFactory.CreateConnection();
        db.Open();

        // Run core migrations via DbUp (for future schema evolution)
        await RunCoreMigrationsAsync(db);

        await _managedEntitySchemaManager.EnsureAsync(db);

        await _extensionRegistry.ApplyAllMigrationsAsync(db);

        await SeedDataAsync(db);

        _logger.LogInformation("Database initialized successfully");
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        // Create connection to master to check/create the database
        var masterCs = _connectionFactory.CreateConnection().ConnectionString;
        var builder = new SqlConnectionStringBuilder(masterCs);
        builder.InitialCatalog = "master";

        using var masterDb = new SqlConnection(builder.ConnectionString);
        masterDb.Open();

        var dbName = "AdminShell";
        var exists = await masterDb.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys.databases WHERE name = @Name",
            new { Name = dbName });

        if (exists == 0)
        {
            await masterDb.ExecuteAsync($"CREATE DATABASE [{dbName}]");
            _logger.LogInformation("Created database {DbName}", dbName);
        }
    }

    private async Task RunCoreMigrationsAsync(IDbConnection db)
    {
        // Fallback inline migrations (for when DbUp scripts don't exist yet)
        const string createRoles = @"
            IF OBJECT_ID(N'Roles', N'U') IS NULL
            CREATE TABLE Roles (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(256) NOT NULL UNIQUE,
                Description NVARCHAR(MAX),
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME2,
                CreatedAt DATETIME2 NOT NULL,
                CreatedBy NVARCHAR(256)
            );";

        const string createUsers = @"
            IF OBJECT_ID(N'Users', N'U') IS NULL
            CREATE TABLE Users (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Email NVARCHAR(256) NOT NULL UNIQUE,
                Username NVARCHAR(128) NOT NULL UNIQUE,
                DisplayName NVARCHAR(256),
                PasswordHash NVARCHAR(MAX) NOT NULL,
                AvatarUrl NVARCHAR(MAX),
                IsActive BIT NOT NULL DEFAULT 1,
                RefreshToken NVARCHAR(MAX),
                RefreshTokenExpiresAt DATETIME2,
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME2,
                CreatedAt DATETIME2 NOT NULL,
                UpdatedAt DATETIME2,
                CreatedBy NVARCHAR(256),
                UpdatedBy NVARCHAR(256)
            );";

        const string createUserRoles = @"
            IF OBJECT_ID(N'UserRoles', N'U') IS NULL
            CREATE TABLE UserRoles (
                UserId UNIQUEIDENTIFIER NOT NULL,
                RoleId UNIQUEIDENTIFIER NOT NULL,
                PRIMARY KEY (UserId, RoleId)
            );";

        const string createPluginInfos = @"
            IF OBJECT_ID(N'PluginInfos', N'U') IS NULL
            CREATE TABLE PluginInfos (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                PluginId NVARCHAR(128) NOT NULL UNIQUE,
                Name NVARCHAR(256) NOT NULL,
                Version NVARCHAR(64) NOT NULL,
                AssemblyPath NVARCHAR(MAX),
                IsEnabled BIT NOT NULL DEFAULT 1,
                Description NVARCHAR(MAX),
                SettingsJson NVARCHAR(MAX),
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME2,
                CreatedAt DATETIME2 NOT NULL,
                CreatedBy NVARCHAR(256)
            );";

        const string createPermissions = @"
            IF OBJECT_ID(N'Permissions', N'U') IS NULL
            CREATE TABLE Permissions (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Code NVARCHAR(128) NOT NULL UNIQUE,
                Description NVARCHAR(MAX),
                Resource NVARCHAR(128) NOT NULL,
                Action NVARCHAR(64) NOT NULL,
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME2,
                CreatedAt DATETIME2 NOT NULL,
                CreatedBy NVARCHAR(256)
            );";

        const string createRolePermissions = @"
            IF OBJECT_ID(N'RolePermissions', N'U') IS NULL
            CREATE TABLE RolePermissions (
                RoleId UNIQUEIDENTIFIER NOT NULL,
                PermissionId UNIQUEIDENTIFIER NOT NULL,
                PRIMARY KEY (RoleId, PermissionId)
            );";

        const string createSettings = @"
            IF OBJECT_ID(N'Settings', N'U') IS NULL
            CREATE TABLE Settings (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                [Key] NVARCHAR(256) NOT NULL UNIQUE,
                Value NVARCHAR(MAX) NOT NULL,
                Category NVARCHAR(128) NOT NULL DEFAULT 'general',
                Description NVARCHAR(MAX),
                ValueType NVARCHAR(32) NOT NULL DEFAULT 'string',
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME2,
                CreatedAt DATETIME2 NOT NULL,
                UpdatedAt DATETIME2,
                CreatedBy NVARCHAR(256),
                UpdatedBy NVARCHAR(256)
            );";

        const string createAuditLogs = @"
            IF OBJECT_ID(N'AuditLogs', N'U') IS NULL
            CREATE TABLE AuditLogs (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Action NVARCHAR(64) NOT NULL,
                EntityType NVARCHAR(128) NOT NULL,
                EntityId NVARCHAR(128),
                PreviousValue NVARCHAR(MAX),
                NewValue NVARCHAR(MAX),
                PerformedBy NVARCHAR(256) NOT NULL,
                IpAddress NVARCHAR(64),
                Details NVARCHAR(MAX),
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME2,
                CreatedAt DATETIME2 NOT NULL,
                CreatedBy NVARCHAR(256)
            );";

        const string createDepartments = @"
            IF OBJECT_ID(N'Departments', N'U') IS NULL
            CREATE TABLE Departments (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(256) NOT NULL,
                Code NVARCHAR(64) NOT NULL UNIQUE,
                Description NVARCHAR(MAX),
                IsActive BIT NOT NULL DEFAULT 1,
                IsDeleted BIT NOT NULL DEFAULT 0,
                CreatedAt DATETIME2 NOT NULL,
                UpdatedAt DATETIME2,
                CreatedBy NVARCHAR(256),
                UpdatedBy NVARCHAR(256)
            );";

        await db.ExecuteAsync(createRoles);
        await db.ExecuteAsync(createUsers);
        await db.ExecuteAsync(createUserRoles);
        await db.ExecuteAsync(createPluginInfos);
        await db.ExecuteAsync(createPermissions);
        await db.ExecuteAsync(createRolePermissions);
        await db.ExecuteAsync(createSettings);
        await db.ExecuteAsync(createAuditLogs);
        await db.ExecuteAsync(createDepartments);
    }

    private async Task SeedDataAsync(IDbConnection db)
    {
        // Check if admin role already exists
        var adminRoleId = await db.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT TOP 1 Id FROM Roles WHERE Name = @Name",
            new { Name = "Admin" });

        var now = DateTime.UtcNow;

        if (!adminRoleId.HasValue)
        {
            var roleAdminId = Guid.NewGuid();
            var roleUserId = Guid.NewGuid();

            await db.ExecuteAsync(
                @"INSERT INTO Roles (Id, Name, Description, IsDeleted, CreatedAt, CreatedBy)
                  VALUES (@Id, @Name, @Desc, 0, @Now, 'system')",
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
                @"INSERT INTO Users (Id, Email, Username, DisplayName, PasswordHash, IsActive, IsDeleted, CreatedAt, CreatedBy)
                  VALUES (@Id, @Email, @Username, @DisplayName, @Pwd, 1, 0, @Now, 'system')",
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
                    @"INSERT INTO Permissions (Id, Code, Description, Resource, Action, IsDeleted, CreatedAt, CreatedBy)
                      VALUES (@Id, @Code, @Description, @Resource, @Action, 0, @Now, 'system')",
                    new { Id = permissionId.Value, permission.Code, permission.Description, permission.Resource, permission.Action, Now = now });
            }

            await db.ExecuteAsync(
                @"INSERT INTO RolePermissions (RoleId, PermissionId)
                  SELECT @RoleId, @PermissionId
                  WHERE NOT EXISTS (
                      SELECT 1 FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId)",
                new { RoleId = adminRoleId, PermissionId = permissionId });
        }

        // Always seed default settings if missing (supports DB upgrades)
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