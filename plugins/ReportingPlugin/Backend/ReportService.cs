using Microsoft.Extensions.Logging;

namespace ReportingPlugin;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;

    public ReportService(ILogger<ReportService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<string>> GetAvailableReportsAsync(CancellationToken ct = default)
    {
        var reports = new List<string> { "UserSummary", "RevenueReport", "ActivityLog" };
        _logger.LogInformation("ReportingPlugin: returning {Count} available reports", reports.Count);
        return Task.FromResult<IReadOnlyList<string>>(reports);
    }

    public async Task<string> GenerateReportAsync(string reportName, CancellationToken ct = default)
    {
        _logger.LogInformation("ReportingPlugin: generating report {Name}", reportName);
        await Task.Delay(100, ct); // Simulate work
        return $"Report '{reportName}' generated at {DateTime.UtcNow:O}";
    }
}
