using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
public class AuditLogController : ApiControllerBase
{
    private readonly IAuditLogRepository _auditLogRepo;

    public AuditLogController(IAuditLogRepository auditLogRepo)
    {
        _auditLogRepo = auditLogRepo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AuditLogEnvelope), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogEnvelope>> Get([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var entries = await _auditLogRepo.GetAllAsync(skip, take, ct);
        var total = await _auditLogRepo.GetCountAsync(ct);
        
        return Ok(new
        {
            data = entries.Select(e => new
            {
                e.Id,
                e.Action,
                e.EntityType,
                e.EntityId,
                e.PerformedBy,
                e.Details,
                e.IpAddress,
                Timestamp = e.CreatedAt
            }),
            total
        });
    }

    [HttpGet("action/{action}")]
    [ProducesResponseType(typeof(AuditLogEnvelope), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogEnvelope>> GetByAction(string action, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var entries = await _auditLogRepo.GetByActionAsync(action, skip, take, ct);
        var total = entries.Count;
        
        return Ok(new
        {
            data = entries.Select(e => new
            {
                e.Id,
                e.Action,
                e.EntityType,
                e.EntityId,
                e.PerformedBy,
                e.Details,
                e.IpAddress,
                Timestamp = e.CreatedAt
            }),
            total
        });
    }
}