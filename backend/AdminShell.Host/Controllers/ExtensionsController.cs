using AdminShell.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AdminShell.Host.Controllers;

public record SearchResultEnvelope(
    string Title,
    string? Description,
    string Url,
    string Category,
    string? Icon,
    int Score,
    string ProviderId,
    string ProviderName
);

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
    [ProducesResponseType(typeof(ExtensionRegistrySnapshot), StatusCodes.Status200OK)]
    public ActionResult<ExtensionRegistrySnapshot> GetExtensions()
    {
        return Ok(_registry.GetSnapshot());
    }

    /// <summary>GET /api/extensions/widgets — dashboard widgets from plugins.</summary>
    [HttpGet("widgets")]
    [ProducesResponseType(typeof(List<WidgetDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<WidgetDescriptor>> GetWidgets()
    {
        return Ok(_registry.GetWidgets());
    }

    /// <summary>GET /api/extensions/tabs — tabs for existing pages.</summary>
    [HttpGet("tabs")]
    [ProducesResponseType(typeof(List<TabDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<TabDescriptor>> GetTabs()
    {
        return Ok(_registry.GetTabs());
    }

    /// <summary>GET /api/extensions/form-fields — extra form fields.</summary>
    [HttpGet("form-fields")]
    [ProducesResponseType(typeof(List<FormFieldDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<FormFieldDescriptor>> GetFormFields()
    {
        return Ok(_registry.GetFormFields());
    }

    /// <summary>GET /api/extensions/header-actions — header/toolbar actions.</summary>
    [HttpGet("header-actions")]
    [ProducesResponseType(typeof(List<HeaderActionDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<HeaderActionDescriptor>> GetHeaderActions()
    {
        return Ok(_registry.GetHeaderActions());
    }

    /// <summary>GET /api/extensions/reports — report types.</summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(List<ReportDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<ReportDescriptor>> GetReports()
    {
        return Ok(_registry.GetReports());
    }

    /// <summary>GET /api/extensions/sidebar-sections — sidebar sections.</summary>
    [HttpGet("sidebar-sections")]
    [ProducesResponseType(typeof(List<SidebarSectionDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<SidebarSectionDescriptor>> GetSidebarSections()
    {
        return Ok(_registry.GetSidebarSections());
    }

    /// <summary>GET /api/extensions/menu-items — menu items from plugins.</summary>
    [HttpGet("menu-items")]
    [ProducesResponseType(typeof(List<MenuItem>), StatusCodes.Status200OK)]
    public ActionResult<List<MenuItem>> GetMenuItems()
    {
        return Ok(_registry.GetMenuItems());
    }

    /// <summary>GET /api/extensions/page-resources — page resources.</summary>
    [HttpGet("page-resources")]
    [ProducesResponseType(typeof(List<PageResourceDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<List<PageResourceDescriptor>> GetPageResources()
    {
        return Ok(_registry.GetPageResources());
    }

    /// <summary>POST /api/extensions/refresh — force re-query all plugins.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public ActionResult<MessageResponse> Refresh()
    {
        _registry.Refresh();
        return Ok(new { message = "Extension registry refreshed" });
    }

    /// <summary>GET /api/extensions/search — aggregate plugin search providers.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<SearchResultEnvelope>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SearchResultEnvelope>>> Search(string q, int limit = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());

        var searchProviders = _registry.GetSearchProviders().ToList();
        var results = new List<SearchResultEnvelope>();

        foreach (var provider in searchProviders)
        {
            try
            {
                var providerResults = await provider.SearchAsync(q, limit, ct);
                var providerId = PluginComponentMetadata.GetPluginId(provider);
                results.AddRange(providerResults.Select(item => new SearchResultEnvelope(
                    item.Title,
                    item.Description,
                    item.Url,
                    item.Category,
                    item.Icon,
                    item.Score,
                    providerId,
                    providerId
                )));
            }
            catch (Exception ex)
            {
                var providerId = PluginComponentMetadata.GetPluginId(provider);
                results.Add(new SearchResultEnvelope(
                    $"{providerId} search failed",
                    ex.Message,
                    string.Empty,
                    "Search Error",
                    "Warning",
                    0,
                    providerId,
                    providerId
                ));
            }
        }

        var ordered = results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Title)
            .Take(Math.Max(1, Math.Min(limit, 50)))
            .ToList();

        return Ok(ordered);
    }
}
