using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AdminShell.Host.Controllers;

[ApiController]
[Route("api/plugins/{pluginId}/frontend")]
public class FrontendPluginsController : ControllerBase
{
    private readonly IPluginLoader _pluginLoader;
    private readonly IConfiguration _configuration;

    public FrontendPluginsController(IPluginLoader pluginLoader, IConfiguration configuration)
    {
        _pluginLoader = pluginLoader;
        _configuration = configuration;
    }

    /// <summary>GET /api/plugins/{pluginId}/frontend/manifest.json — embedded or installed frontend manifest.</summary>
    [HttpGet("manifest.json")]
    [ProducesResponseType(typeof(EmbeddedFrontendManifest), StatusCodes.Status200OK)]
    public ActionResult<EmbeddedFrontendManifest> GetFrontendManifest(string pluginId)
    {
        var embeddedManifest = _pluginLoader.GetEmbeddedFrontendManifest(pluginId);
        if (embeddedManifest is not null)
        {
            return Ok(embeddedManifest);
        }

        var manifestPath = GetInstalledPluginManifestPath(pluginId);
        if (manifestPath is null)
        {
            return NotFound();
        }

        return new PhysicalFileResult(manifestPath, "application/json; charset=utf-8");
    }

    /// <summary>GET /api/plugins/{pluginId}/frontend/{**path} — embedded or installed frontend asset.</summary>
    [HttpGet("{**path}")]
    public IActionResult GetFrontendAsset(string pluginId, string path)
    {
        var asset = _pluginLoader.GetEmbeddedFrontendAsset(pluginId, path);
        if (asset is not null)
        {
            return new FileContentResult(asset.Content, asset.ContentType);
        }

        var filePath = ResolveInstalledFrontendAssetPath(pluginId, path);
        if (filePath is null)
        {
            return NotFound();
        }

        return new PhysicalFileResult(filePath, GetContentType(filePath));
    }

    private string? GetInstalledPluginManifestPath(string pluginId)
    {
        if (!IsPluginAvailable(pluginId))
        {
            return null;
        }

        var pluginDirectory = GetInstalledPluginDirectory(pluginId);
        if (pluginDirectory is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(pluginDirectory, "manifest.json");
        return System.IO.File.Exists(manifestPath) ? manifestPath : null;
    }

    private string? ResolveInstalledFrontendAssetPath(string pluginId, string path)
    {
        if (!IsPluginAvailable(pluginId))
        {
            return null;
        }

        var pluginDirectory = GetInstalledPluginDirectory(pluginId);
        if (pluginDirectory is null)
        {
            return null;
        }

        var normalized = path.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return GetInstalledPluginManifestPath(pluginId);
        }

        if (normalized.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("plugin.json", StringComparison.OrdinalIgnoreCase))
        {
            return GetInstalledPluginManifestPath(pluginId);
        }

        var frontendDirectory = Path.Combine(pluginDirectory, "frontend");
        if (!Directory.Exists(frontendDirectory))
        {
            return null;
        }

        var filePath = Path.GetFullPath(Path.Combine(frontendDirectory, normalized));
        var frontendRoot = Path.GetFullPath(frontendDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!filePath.StartsWith(frontendRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return System.IO.File.Exists(filePath) ? filePath : null;
    }

    private string? GetInstalledPluginDirectory(string pluginId)
    {
        if (!IsSafePluginId(pluginId))
        {
            return null;
        }

        var pluginsDirectory = _configuration["Plugins:Directory"] ?? "plugins";
        var pluginDirectory = Path.GetFullPath(Path.Combine(pluginsDirectory, pluginId));
        var pluginsRoot = Path.GetFullPath(pluginsDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!pluginDirectory.StartsWith(pluginsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return Directory.Exists(pluginDirectory) ? pluginDirectory : null;
    }

    private bool IsPluginAvailable(string pluginId)
    {
        return _pluginLoader.LoadedPlugins.Any(p =>
            p.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase)
            && (p.Status == PluginStatus.Active || p.Status == PluginStatus.Loaded));
    }

    private static bool IsSafePluginId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            return false;
        }

        return pluginId.All(ch =>
            char.IsLetterOrDigit(ch)
            || ch is '.' or '_' or '-');
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".css" => "text/css; charset=utf-8",
            ".js" or ".mjs" => "text/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".map" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };
    }
}
