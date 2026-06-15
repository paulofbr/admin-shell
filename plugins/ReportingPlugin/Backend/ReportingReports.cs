using AdminShell.Contracts;

namespace ReportingPlugin;

[PluginComponent("reporting")]
public sealed class ReportingReports : IReportPlugin
{
    public IEnumerable<ReportDescriptor> GetReports()
    {
        yield return new ReportDescriptor
        {
            Id = "user-growth",
            Name = "User Growth Report",
            Description = "Shows user registration trends over time",
            Icon = "TrendCharts",
            Category = "users",
            SupportedFormats = new[] { "json", "csv", "pdf" },
            ReportEndpoint = "/api/plugins/reporting/reports/generate/user-growth",
            Order = 10,
            RequiredPermissions = new[] { "users:read" }
        };

        yield return new ReportDescriptor
        {
            Id = "audit-summary",
            Name = "Audit Log Summary",
            Description = "Summary of system actions and user activity",
            Icon = "DataAnalysis",
            Category = "audit",
            SupportedFormats = new[] { "json", "csv" },
            ReportEndpoint = "/api/plugins/reporting/reports/generate/audit-summary",
            Order = 20,
            RequiredPermissions = new[] { "audit:read" }
        };

        yield return new ReportDescriptor
        {
            Id = "role-permissions",
            Name = "Roles & Permissions Audit",
            Description = "Matrix of roles and their assigned permissions",
            Icon = "Setting",
            Category = "system",
            SupportedFormats = new[] { "json", "csv", "pdf" },
            ReportEndpoint = "/api/plugins/reporting/reports/generate/role-permissions",
            Order = 30
        };

        yield return new ReportDescriptor
        {
            Id = "security-overview",
            Name = "Security Overview",
            Description = "Login attempts, failed logins, and active sessions",
            Icon = "Lock",
            Category = "security",
            SupportedFormats = new[] { "json", "csv", "pdf" },
            ReportEndpoint = "/api/plugins/reporting/reports/generate/security-overview",
            Order = 40,
            RequiredPermissions = new[] { "security:read" }
        };
    }
}
