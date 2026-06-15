namespace ReportingPlugin;

public interface IReportService
{
    Task<IReadOnlyList<string>> GetAvailableReportsAsync(CancellationToken ct = default);
    Task<string> GenerateReportAsync(string reportName, CancellationToken ct = default);
}
