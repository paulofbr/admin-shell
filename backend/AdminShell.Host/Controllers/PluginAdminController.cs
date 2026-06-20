using AdminShell.Contracts;
using System.Text.Json;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/plugins")]
public class PluginAdminController : ApiControllerBase
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IPluginInstaller _pluginInstaller;
    private readonly ISettingsRegistry _settingsRegistry;

    public PluginAdminController(IPluginLoader pluginLoader, IPluginInstaller pluginInstaller, ISettingsRegistry settingsRegistry)
    {
        _pluginLoader = pluginLoader;
        _pluginInstaller = pluginInstaller;
        _settingsRegistry = settingsRegistry;
    }

    [HttpPost("install")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    [ProducesResponseType(typeof(PluginInstallResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PluginInstallResult>> Install(
        IFormFile file,
        [FromForm] bool activate = true,
        CancellationToken ct = default)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { Message = "A plugin .zip file is required." });
            }

            await using var stream = file.OpenReadStream();
            var result = await _pluginInstaller.InstallAsync(stream, file.FileName, file.Length, activate, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PluginDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<PluginDescriptor>> GetAll()
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

    [HttpGet("{pluginId}/settings")]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> GetSettings(string pluginId, CancellationToken ct)
    {
        try
        {
            return Ok(await _settingsRegistry.GetSettingsAsync(pluginId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPut("{pluginId}/settings")]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> UpdateSettings(
        string pluginId,
        [FromBody] List<UpdateSettingRequest> requests,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _settingsRegistry.UpdateAsync(pluginId, requests, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{pluginId}/enable")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> Enable(string pluginId, CancellationToken ct)
    {
        var result = await _pluginLoader.EnablePluginAsync(pluginId, ct);
        if (!result) return NotFound(new { Message = $"Plugin '{pluginId}' not found" });
        return Ok(new { Message = $"Plugin '{pluginId}' enabled" });
    }

    [HttpPost("{pluginId}/disable")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> Disable(string pluginId, CancellationToken ct)
    {
        var result = await _pluginLoader.DisablePluginAsync(pluginId, ct);
        if (!result) return NotFound(new { Message = $"Plugin '{pluginId}' not found" });
        return Ok(new { Message = $"Plugin '{pluginId}' disabled" });
    }
}