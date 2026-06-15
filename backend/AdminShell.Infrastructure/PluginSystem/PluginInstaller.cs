using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.PluginSystem;

public sealed class PluginInstaller : IPluginInstaller
{
    private const long DefaultMaxPackageSizeBytes = 50L * 1024L * 1024L;
    private static readonly Regex PluginIdRegex = new(
        "^[a-z0-9][a-z0-9._-]{0,127}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IPluginLoader _pluginLoader;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PluginInstaller> _logger;
    private readonly string _pluginsDirectory;

    public PluginInstaller(
        IPluginLoader pluginLoader,
        IConfiguration configuration,
        ILogger<PluginInstaller> logger)
    {
        _pluginLoader = pluginLoader;
        _configuration = configuration;
        _logger = logger;
        _pluginsDirectory = configuration["Plugins:Directory"] ?? "plugins";
    }

    public async Task<PluginInstallResult> InstallAsync(
        Stream zipStream,
        string fileName,
        long length,
        bool activate,
        CancellationToken ct = default)
    {
        ValidatePackage(fileName, length);

        Directory.CreateDirectory(_pluginsDirectory);

        var tempRoot = Path.Combine(Path.GetTempPath(), "adminshell-plugin-install", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            await ExtractZipAsync(zipStream, tempRoot, ct);

            var manifestPath = FindManifestPath(tempRoot);
            if (manifestPath is null)
            {
                throw new InvalidOperationException("Plugin package is missing manifest.json.");
            }

            var manifest = ReadManifest(manifestPath);
            ValidatePluginId(manifest.PluginId);

            var pluginDirectory = Path.Combine(_pluginsDirectory, manifest.PluginId);
            if (Directory.Exists(pluginDirectory))
            {
                throw new InvalidOperationException(
                    $"Plugin '{manifest.PluginId}' is already installed at '{pluginDirectory}'.");
            }

            var backendSource = FindCaseInsensitiveDirectory(tempRoot, "backend")
                ?? FindCaseInsensitiveDirectory(tempRoot, "Backend");

            if (backendSource is null)
            {
                throw new InvalidOperationException("Plugin package is missing the backend/ directory.");
            }

            Directory.CreateDirectory(pluginDirectory);

            try
            {
                File.Copy(manifestPath, Path.Combine(pluginDirectory, "manifest.json"), overwrite: false);
                CopyDirectory(backendSource, Path.Combine(pluginDirectory, "backend"), ct);

                var frontendSource = FindCaseInsensitiveDirectory(tempRoot, "frontend")
                    ?? FindCaseInsensitiveDirectory(tempRoot, "FrontEnd")
                    ?? FindCaseInsensitiveDirectory(tempRoot, "Frontend");

                if (frontendSource is not null)
                {
                    CopyDirectory(frontendSource, Path.Combine(pluginDirectory, "frontend"), ct);
                }
            }
            catch
            {
                try
                {
                    Directory.Delete(pluginDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup failures; the original exception is more important.
                }

                throw;
            }

            await _pluginLoader.LoadPluginsAsync(_pluginsDirectory, ct);

            var descriptor = _pluginLoader.LoadedPlugins.FirstOrDefault(p =>
                p.Id.Equals(manifest.PluginId, StringComparison.OrdinalIgnoreCase));

            if (descriptor is null)
            {
                throw new InvalidOperationException(
                    $"Plugin '{manifest.PluginId}' was copied but not discovered by the plugin loader.");
            }

            var messages = new List<string>
            {
                $"Installed plugin '{manifest.PluginId}' v{manifest.Version}."
            };

            var activated = false;
            if (activate)
            {
                if (descriptor.Status == PluginStatus.Failed)
                {
                    messages.Add($"Plugin was not activated because dependencies are unresolved: {descriptor.ErrorMessage}");
                }
                else
                {
                    var enableResult = await _pluginLoader.EnablePluginAsync(manifest.PluginId, ct);
                    activated = enableResult;
                    messages.Add(enableResult ? "Plugin activated." : "Plugin was loaded but could not be activated.");
                }
            }
            else
            {
                messages.Add("Plugin installed. Activate it from the Plugins page when ready.");
            }

            _logger.LogInformation(
                "Installed plugin {PluginId} v{Version} from {FileName}. Activated={Activated}",
                manifest.PluginId,
                manifest.Version,
                fileName,
                activated);

            return new PluginInstallResult(
                manifest.PluginId,
                manifest.Name,
                manifest.Version,
                pluginDirectory,
                activated,
                messages);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to delete temporary plugin install directory {Dir}", tempRoot);
            }
        }
    }

    private void ValidatePackage(string fileName, long length)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Plugin package file name is required.");
        }

        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Plugin package must be a .zip file.");
        }

        if (length <= 0)
        {
            throw new InvalidOperationException("Plugin package is empty.");
        }

        var maxPackageSizeBytes = _configuration.GetValue<long>(
            "Plugins:Install:MaxPackageSizeBytes",
            DefaultMaxPackageSizeBytes);

        if (length > maxPackageSizeBytes)
        {
            throw new InvalidOperationException(
                $"Plugin package is too large. Maximum allowed size is {maxPackageSizeBytes / 1024 / 1024} MB.");
        }
    }

    private static async Task ExtractZipAsync(Stream zipStream, string destinationDirectory, CancellationToken ct)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();

            var destinationPath = ResolveArchiveEntryPath(destinationDirectory, entry.FullName);
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            await using var entryStream = entry.Open();
            await using var fileStream = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await entryStream.CopyToAsync(fileStream, ct);
        }
    }

    private static string ResolveArchiveEntryPath(string destinationDirectory, string entryName)
    {
        var normalizedEntryName = entryName.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalizedEntryName))
        {
            return destinationDirectory;
        }

        var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, normalizedEntryName));
        var destinationRoot = Path.GetFullPath(destinationDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!destinationPath.StartsWith(destinationRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !destinationPath.Equals(destinationRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsafe path detected in plugin package: {entryName}");
        }

        return destinationPath;
    }

    private static string? FindManifestPath(string packageRoot)
    {
        var candidates = Directory.EnumerateFiles(packageRoot, "manifest.json", SearchOption.AllDirectories).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var topLevel = candidates.FirstOrDefault(path =>
            Path.GetDirectoryName(path)?.Equals(packageRoot, StringComparison.OrdinalIgnoreCase) == true);

        if (topLevel is not null)
        {
            return topLevel;
        }

        var topLevelDirectories = Directory.EnumerateDirectories(packageRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nested = candidates
            .Where(path =>
            {
                var relative = Path.GetRelativePath(packageRoot, path)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                return relative.Length == 2 && topLevelDirectories.Contains(relative[0]);
            })
            .ToList();

        if (nested.Count == 1)
        {
            return nested[0];
        }

        throw new InvalidOperationException("Plugin package must contain exactly one manifest.json.");
    }

    private static PluginManifestInfo ReadManifest(string manifestPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        return new PluginManifestInfo(
            RequireString(root, "id"),
            RequireString(root, "name"),
            root.TryGetProperty("version", out var version) ? RequireString(root, "version") : "1.0.0",
            root.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty);
    }

    private static string RequireString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            throw new JsonException($"Plugin manifest is missing required property '{propertyName}'.");
        }

        var value = property.ValueKind == JsonValueKind.String ? property.GetString() : null;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Plugin manifest property '{propertyName}' must be a non-empty string.");
        }

        return value;
    }

    private static void ValidatePluginId(string pluginId)
    {
        if (!PluginIdRegex.IsMatch(pluginId))
        {
            throw new InvalidOperationException(
                "Plugin id must contain only letters, numbers, dots, underscores or hyphens, and start with a letter or number.");
        }
    }

    private static string? FindCaseInsensitiveDirectory(string root, string directoryName)
    {
        return Directory.EnumerateDirectories(root)
            .FirstOrDefault(path => Path.GetFileName(path)?.Equals(directoryName, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory, CancellationToken ct)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory))
        {
            ct.ThrowIfCancellationRequested();
            var destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(file));
            File.Copy(file, destinationFile, overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
        {
            ct.ThrowIfCancellationRequested();
            CopyDirectory(
                directory,
                Path.Combine(destinationDirectory, Path.GetFileName(directory)),
                ct);
        }
    }

    private sealed record PluginManifestInfo(string PluginId, string Name, string Version, string Description);
}
