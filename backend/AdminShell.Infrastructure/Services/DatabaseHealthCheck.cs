using AdminShell.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdminShell.Infrastructure.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            using var db = _connectionFactory.CreateConnection();
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteNonQuery();
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
