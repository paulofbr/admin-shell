using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/plugins")]
public class PluginAdminController : ControllerBase
{
    private readonly IPluginLoader _pluginLoader;

    public PluginAdminController(IPluginLoader pluginLoader)
    {
        _pluginLoader = pluginLoader;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_pluginLoader.LoadedPlugins.Select(p => new
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
        }));
    }

    [HttpPost("{pluginId}/enable")]
    public async Task<IActionResult> Enable(string pluginId, CancellationToken ct)
    {
        var result = await _pluginLoader.EnablePluginAsync(pluginId, ct);
        if (!result) return NotFound(new { Message = $"Plugin '{pluginId}' not found" });
        return Ok(new { Message = $"Plugin '{pluginId}' enabled" });
    }

    [HttpPost("{pluginId}/disable")]
    public async Task<IActionResult> Disable(string pluginId, CancellationToken ct)
    {
        var result = await _pluginLoader.DisablePluginAsync(pluginId, ct);
        if (!result) return NotFound(new { Message = $"Plugin '{pluginId}' not found" });
        return Ok(new { Message = $"Plugin '{pluginId}' disabled" });
    }
}