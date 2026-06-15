using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using AdminShell.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IPluginLoader pluginLoader, IHealthCheckService healthCheckService)
    {
        _pluginLoader = pluginLoader;
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthStatusResponse>> Get()
    {
        var healthResults = await _healthCheckService.CheckAllAsync();
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Plugins = _pluginLoader.LoadedPlugins.Select(p => new
            {
                p.Id,
                p.Name,
                p.Version,
                p.Description,
                p.Status,
                p.ErrorMessage,
                p.LoadedAt,
                Dependencies = p.Dependencies.Select(d => new
                {
                    d.PluginId,
                    d.VersionConstraint,
                    d.IsOptional,
                    d.IsResolved,
                    d.ErrorMessage
                })
            }),
            Checks = healthResults.Select(r => new
            {
                r.Name,
                r.Status,
                r.Description,
                r.Duration,
                r.ErrorMessage,
                r.Data
            })
        });
    }
}
