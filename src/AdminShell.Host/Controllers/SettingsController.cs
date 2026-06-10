using AdminShell.Core.Entities;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsRepository _settingsRepository;

    public SettingsController(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var settings = await _settingsRepository.GetAllAsync(ct);
        return Ok(settings.Select(s => new
        {
            s.Key, s.Value, s.Category, s.Description, s.ValueType
        }));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _settingsRepository.GetCategoriesAsync(ct);
        return Ok(categories);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var settings = await _settingsRepository.GetByCategoryAsync(category, ct);
        return Ok(settings.Select(s => new
        {
            s.Key, s.Value, s.Category, s.Description, s.ValueType
        }));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
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
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request, CancellationToken ct)
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
    public async Task<IActionResult> UpdateBatch([FromBody] List<UpdateSettingRequest> requests, CancellationToken ct)
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

public record UpdateSettingRequest(string Key, string Value);