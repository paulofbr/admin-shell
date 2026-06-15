using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace ReportingPlugin;

[PluginComponent(PluginId)]
public sealed class ReportingApi : IApiPlugin
{
    private const string PluginId = "reporting";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ReportingPlugin");

        var group = endpoints.MapPluginApi(PluginId);

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
        .WithName("GetReports")
        .Produces<List<ReportDto>>(StatusCodes.Status200OK);

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
        .WithName("GetWidgetSummary")
        .Produces<WidgetSummaryDto>(StatusCodes.Status200OK);

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
        .WithName("GenerateReport")
        .Produces<GenerateReportResponse>(StatusCodes.Status200OK);
    }
}
