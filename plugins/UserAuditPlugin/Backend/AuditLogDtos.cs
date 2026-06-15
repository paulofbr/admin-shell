namespace UserAuditPlugin;

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

public sealed record AuditLogEnvelope(IReadOnlyList<AuditLogDto> Data, int Total);
