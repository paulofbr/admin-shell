using AdminShell.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

[ApiController]
[Route("api/extensions")]
public class ExtensionsController : ControllerBase
{
    private readonly IPluginExtensionRegistry _registry;

    public ExtensionsController(IPluginExtensionRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>GET /api/extensions — full snapshot of all plugin extensions.</summary>
    [HttpGet]
    public IActionResult GetExtensions()
    {
        return Ok(_registry.GetSnapshot());
    }

    /// <summary>GET /api/extensions/widgets — dashboard widgets from plugins.</summary>
    [HttpGet("widgets")]
    public IActionResult GetWidgets()
    {
        return Ok(_registry.GetWidgets());
    }

    /// <summary>GET /api/extensions/tabs — tabs for existing pages.</summary>
    [HttpGet("tabs")]
    public IActionResult GetTabs()
    {
        return Ok(_registry.GetTabs());
    }

    /// <summary>GET /api/extensions/form-fields — extra form fields.</summary>
    [HttpGet("form-fields")]
    public IActionResult GetFormFields()
    {
        return Ok(_registry.GetFormFields());
    }

    /// <summary>GET /api/extensions/header-actions — header/toolbar actions.</summary>
    [HttpGet("header-actions")]
    public IActionResult GetHeaderActions()
    {
        return Ok(_registry.GetHeaderActions());
    }

    /// <summary>GET /api/extensions/reports — report types.</summary>
    [HttpGet("reports")]
    public IActionResult GetReports()
    {
        return Ok(_registry.GetReports());
    }

    /// <summary>GET /api/extensions/sidebar-sections — sidebar sections.</summary>
    [HttpGet("sidebar-sections")]
    public IActionResult GetSidebarSections()
    {
        return Ok(_registry.GetSidebarSections());
    }

    /// <summary>GET /api/extensions/menu-items — menu items from plugins.</summary>
    [HttpGet("menu-items")]
    public IActionResult GetMenuItems()
    {
        return Ok(_registry.GetMenuItems());
    }

    /// <summary>GET /api/extensions/page-resources — page resources.</summary>
    [HttpGet("page-resources")]
    public IActionResult GetPageResources()
    {
        return Ok(_registry.GetPageResources());
    }

    /// <summary>POST /api/extensions/refresh — force re-query all plugins.</summary>
    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        _registry.Refresh();
        return Ok(new { message = "Extension registry refreshed" });
    }
}