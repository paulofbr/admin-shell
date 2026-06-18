namespace AdminShell.Contracts;

public sealed record MessageResponse(string Message);

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Resource,
    string Action,
    string? Description = null
);

public sealed record RolePermissionsResponse(
    Guid RoleId,
    IReadOnlyList<PermissionDto> Assigned,
    IReadOnlyList<PermissionDto> Available
);

public sealed record SettingDto(
    string Key,
    string Value,
    string Category,
    string? Description,
    string? ValueType,
    DateTime? UpdatedAt = null,
    string? UpdatedBy = null
);

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    string? EntityId,
    string? PerformedBy,
    string? Details,
    string? IpAddress,
    DateTime Timestamp
);

public sealed record AuditLogEnvelope(
    IReadOnlyList<AuditLogDto> Data,
    int Total
);

public sealed record LogEntryDto(
    string? Timestamp,
    string Level,
    string? Source,
    string Message,
    string? Exception
);

public sealed record LogFilePageDto(
    IReadOnlyList<LogEntryDto> Data,
    bool HasMore,
    int ScannedBytes,
    string? Warning
);

public sealed record LogQueryDto(
    int Skip,
    int Take,
    string? Type,
    string? Message
);

public sealed record MonthlyGrowthResponse(string Month, int Count);

public sealed record AuditActionCountResponse(string Action, int Count);

public sealed record DashboardMetricsResponse(
    DashboardUsersMetrics Users,
    DashboardRolesMetrics Roles,
    DashboardPluginsMetrics Plugins,
    DashboardAuditMetrics Audit
);

public sealed record DashboardUsersMetrics(
    int Total,
    int Active,
    int Inactive,
    IReadOnlyList<MonthlyGrowthResponse> MonthlyGrowth
);

public sealed record DashboardRolesMetrics(int Total);

public sealed record DashboardPluginsMetrics(int Total, int Active);

public sealed record DashboardAuditMetrics(
    int Today,
    int LoginsToday,
    int FailedLoginsToday,
    IReadOnlyList<AuditActionCountResponse> ByAction
);

public sealed record PluginDependencyDto(
    string PluginId,
    string VersionConstraint,
    bool IsOptional,
    bool IsResolved,
    string? ErrorMessage = null
);

public sealed record PluginHealthResponse(
    string Id,
    string Name,
    string Version,
    string Description,
    string Status,
    string? ErrorMessage,
    DateTime LoadedAt,
    IReadOnlyList<PluginDependencyDto> Dependencies
);

public sealed record HealthCheckStatusResponse(
    string Name,
    string Status,
    string? Description,
    TimeSpan Duration,
    string? ErrorMessage,
    IReadOnlyDictionary<string, string>? Data
);

public sealed record HealthStatusResponse(
    string Status,
    DateTime Timestamp,
    string Version,
    IReadOnlyList<PluginHealthResponse> Plugins,
    IReadOnlyList<HealthCheckStatusResponse> Checks
);
