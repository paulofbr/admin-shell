namespace ReportingPlugin;

public sealed record ReportDto(int Id, string Title, string Type, DateTime CreatedAt);

public sealed record WidgetSummaryDto(
    int TotalReports,
    int ChartsGenerated,
    DateTime LastExport,
    int ActiveDashboards
);

public sealed record GenerateReportResponse(
    string ReportId,
    string Format,
    DateTime GeneratedAt,
    GenerateReportData Data
);

public sealed record GenerateReportData(string Message);
