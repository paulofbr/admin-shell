-- 001: Create core tables
IF OBJECT_ID(N'Roles', N'U') IS NULL
CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    NormalizedName AS UPPER(Name) PERSISTED,
    Description NVARCHAR(MAX),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256)
);

IF OBJECT_ID(N'Users', N'U') IS NULL
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL,
    Username NVARCHAR(128) NOT NULL,
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
);

IF OBJECT_ID(N'UserRoles', N'U') IS NULL
CREATE TABLE UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

IF OBJECT_ID(N'PluginInfos', N'U') IS NULL
CREATE TABLE PluginInfos (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PluginId NVARCHAR(128) NOT NULL,
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
);

IF OBJECT_ID(N'Permissions', N'U') IS NULL
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Code NVARCHAR(128) NOT NULL,
    Description NVARCHAR(MAX),
    Resource NVARCHAR(128) NOT NULL,
    Action NVARCHAR(64) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256)
);

IF OBJECT_ID(N'RolePermissions', N'U') IS NULL
CREATE TABLE RolePermissions (
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id)
);

IF OBJECT_ID(N'Settings', N'U') IS NULL
CREATE TABLE Settings (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    [Key] NVARCHAR(256) NOT NULL,
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
);

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
);

IF OBJECT_ID(N'Departments', N'U') IS NULL
CREATE TABLE Departments (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    Code NVARCHAR(64) NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    CreatedBy NVARCHAR(256),
    UpdatedBy NVARCHAR(256)
);

-- Add unique indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name')
    CREATE UNIQUE INDEX IX_Roles_Name ON Roles(Name) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email')
    CREATE UNIQUE INDEX IX_Users_Email ON Users(Email) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username')
    CREATE UNIQUE INDEX IX_Users_Username ON Users(Username) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_Code')
    CREATE UNIQUE INDEX IX_Permissions_Code ON Permissions(Code) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Settings_Key')
    CREATE UNIQUE INDEX IX_Settings_Key ON Settings([Key]) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PluginInfos_PluginId')
    CREATE UNIQUE INDEX IX_PluginInfos_PluginId ON PluginInfos(PluginId) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_Code')
    CREATE UNIQUE INDEX IX_Departments_Code ON Departments(Code) WHERE IsDeleted = 0;
