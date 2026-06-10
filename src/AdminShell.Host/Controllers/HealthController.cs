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
    public async Task<IActionResult> Get()
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
                p.Status,
                p.ErrorMessage
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
