namespace AdminShell.Core.Entities;

/// <summary>
/// Represents a monthly growth data point for dashboard charts.
/// </summary>
public record MonthlyGrowthPoint(string Month, int Count);

/// <summary>
/// Represents an audit action count for dashboard breakdown.
/// </summary>
public record AuditActionCount(string Action, int Count);