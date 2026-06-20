using AdminShell.Contracts;
using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
public class SettingsController : ApiControllerBase
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISettingsRegistry _settingsRegistry;

    public SettingsController(ISettingsRepository settingsRepository, ISettingsRegistry settingsRegistry)
    {
        _settingsRepository = settingsRepository;
        _settingsRegistry = settingsRegistry;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SettingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SettingDto>>> GetAll(CancellationToken ct)
    {
        var settings = await _settingsRepository.GetAllAsync(ct);
        return Ok(settings.Select(s => new
        {
            s.Key, s.Value, s.Category, s.Description, s.ValueType
        }));
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetCategories(CancellationToken ct)
    {
        var categories = await _settingsRepository.GetCategoriesAsync(ct);
        return Ok(categories);
    }

    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(List<SettingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SettingDto>>> GetByCategory(string category, CancellationToken ct)
    {
        var settings = await _settingsRepository.GetByCategoryAsync(category, ct);
        return Ok(settings.Select(s => new
        {
            s.Key, s.Value, s.Category, s.Description, s.ValueType
        }));
    }

    [HttpGet("options/{category}")]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> GetOptions(string category, CancellationToken ct)
    {
        try
        {
            return Ok(await _settingsRegistry.GetSettingsAsync(category, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [HttpPut("options/{category}")]
    [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> UpdateOptions(
        string category,
        [FromBody] List<UpdateSettingRequest> requests,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _settingsRegistry.UpdateAsync(category, requests, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(SettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingDto>> GetByKey(string key, CancellationToken ct)
    {
        var setting = await _settingsRepository.GetByKeyAsync(key, ct);
        if (setting is null) return NotFound();
        return Ok(new
        {
            setting.Key, setting.Value, setting.Category,
            setting.Description, setting.ValueType
        });
    }

    [HttpPut("{key}")]
    [ProducesResponseType(typeof(SettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingDto>> Update(string key, [FromBody] UpdateSettingRequest request, CancellationToken ct)
    {
        var existing = await _settingsRepository.GetByKeyAsync(key, ct);
        if (existing is null) return NotFound();

        existing.Value = request.Value;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = User.Identity?.Name ?? "system";

        var updated = await _settingsRepository.SetAsync(existing, ct);
        return Ok(new
        {
            updated.Key, updated.Value, updated.Category,
            updated.Description, updated.ValueType
        });
    }

    [HttpPut]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> UpdateBatch([FromBody] List<UpdateSettingRequest> requests, CancellationToken ct)
    {
        var settings = requests.Select(r => new AppSetting
        {
            Key = r.Key,
            Value = r.Value,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = User.Identity?.Name ?? "system"
        });

        await _settingsRepository.SetBatchAsync(settings, ct);
        return Ok(new { Message = "Settings updated" });
    }
}
