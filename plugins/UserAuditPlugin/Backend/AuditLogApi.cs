using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace UserAuditPlugin;

[PluginComponent(PluginId)]
public sealed class AuditLogApi : IApiPlugin
{
    private const string PluginId = "useraudit";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("UserAuditPlugin");

        var group = endpoints.MapPluginApi(PluginId);

        group.MapGet("/audit", async (CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/useraudit/audit called");
            var auditLog = endpoints.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logs = await auditLog.GetRecentAsync(50, 0, ct);
            var total = await auditLog.GetTotalCountAsync(ct);

            return Results.Ok(new
            {
                Data = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.EntityType,
                    l.EntityId,
                    l.PerformedBy,
                    l.Details,
                    l.IpAddress,
                    Timestamp = l.CreatedAt
                }),
                Total = total
            });
        })
        .WithName("GetAuditTrail")
        .Produces<AuditLogEnvelope>(StatusCodes.Status200OK);

        group.MapGet("/audit/{action}", async (string action, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/useraudit/audit/{Action} called", action);
            var auditLog = endpoints.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logs = await auditLog.GetByActionAsync(action, 50, 0, ct);

            return Results.Ok(new
            {
                Data = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.EntityType,
                    l.EntityId,
                    l.PerformedBy,
                    l.Details,
                    l.IpAddress,
                    Timestamp = l.CreatedAt
                }),
                Total = logs.Count
            });
        })
        .WithName("GetAuditTrailByAction")
        .Produces<AuditLogEnvelope>(StatusCodes.Status200OK);
    }
}
