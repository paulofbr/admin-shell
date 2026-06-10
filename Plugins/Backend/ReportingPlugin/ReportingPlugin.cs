using AdminShell.Contracts;

namespace ReportingPlugin;

public class ReportingPlugin : IAdminShellPlugin, IApiPlugin, IWidgetPlugin, IMenuPlugin, IReportPlugin, IHeaderActionPlugin
{
    public string Id => "reporting";
    public string Name => "Reporting Plugin";
    public string Version => "1.0.0";
    public string Description => "Provides reporting capabilities with widgets, reports API, and menu items";

    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ReportingPlugin");
        logger.LogInformation("ReportingPlugin initialized — widgets + menu + API + reports + toolbar ready");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    // ───── IApiPlugin ─────

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ReportingPlugin");

        var group = endpoints.MapGroup("/api/plugins/reporting")
            .WithTags("Reporting (Plugin)");

        group.MapGet("/reports", async (CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/reporting/reports called");
            await Task.Delay(50, ct);
            var reports = new[]
            {
                new { Id = 1, Title = "Monthly Revenue", Type = "Chart", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new { Id = 2, Title = "User Growth", Type = "Chart", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new { Id = 3, Title = "Top Products", Type = "Table", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };
            return Results.Ok(reports);
        })
        .WithName("GetReports");

        group.MapGet("/widgets/summary", async (CancellationToken ct) =>
        {
            await Task.Delay(30, ct);
            return Results.Ok(new
            {
                TotalReports = 3,
                ChartsGenerated = 2,
                LastExport = DateTime.UtcNow.AddMinutes(-15),
                ActiveDashboards = 1
            });
        })
        .WithName("GetWidgetSummary");

        // Report generation endpoint
        group.MapGet("/reports/generate/{reportId}", async (string reportId, string? format, CancellationToken ct) =>
        {
            logger.LogInformation("Generating report {ReportId} as {Format}", reportId, format ?? "json");
            await Task.Delay(100, ct);
            return Results.Ok(new
            {
                ReportId = reportId,
                Format = format ?? "json",
                GeneratedAt = DateTime.UtcNow,
                Data = new { Message = $"Report {reportId} generated successfully" }
            });
        })
        .WithName("GenerateReport");
    }

    // ───── IWidgetPlugin ─────

    public IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor
        {
            Id = "reporting-summary",
            Title = "Reports Summary",
            Zone = "dashboard",
            Width = 4,
            Height = 3,
            Order = 10,
            Settings = new Dictionary<string, object>
            {
                ["refreshInterval"] = 60,
                ["showCharts"] = true
            }
        };

        yield return new WidgetDescriptor
        {
            Id = "reporting-recent",
            Title = "Recent Reports",
            Zone = "dashboard",
            Width = 4,
            Height = 4,
            Order = 20
        };
    }

    // ───── IMenuPlugin ─────

    public IEnumerable<MenuItem> GetMenuItems()
    {
        yield return new MenuItem
        {
            Id = "reports",
            Label = "Reports",
            Path = "/reports",
            Icon = "Document",
            Order = 30
        };

        yield return new MenuItem
        {
            Id = "reports-dashboard",
            Label = "Dashboard",
            Path = "/reports/dashboard",
            Icon = "DataAnalysis",
            Order = 31,
            ParentId = "reports"
        };

        yield return new MenuItem
        {
            Id = "reports-analytics",
            Label = "Analytics",
            Path = "/reports/analytics",
            Icon = "TrendCharts",
            Order = 32,
            ParentId = "reports"
        };
    }

    // ───── IReportPlugin ─────

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

    // ───── IHeaderActionPlugin ─────

    public IEnumerable<HeaderActionDescriptor> GetHeaderActions()
    {
        yield return new HeaderActionDescriptor
        {
            Id = "quick-report",
            Label = "New Report",
            Icon = "Plus",
            Target = "header",
            ActionType = "route",
            ActionValue = "/reports/create",
            Order = 10,
            RequiredPermissions = new[] { "reports:create" }
        };

        yield return new HeaderActionDescriptor
        {
            Id = "export-data",
            Label = "Export Data",
            Icon = "Download",
            Target = "page.toolbar",
            TargetPage = "/users",
            ActionType = "modal",
            ActionValue = "ExportDataModal",
            Order = 10,
            RequiredPermissions = new[] { "users:read" }
        };
    }
}