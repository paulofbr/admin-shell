using System.Reflection;
using System.Text;
using System.Text.Json;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.PluginSystem;

public class PluginLoader : IPluginLoader
{
    private readonly List<PluginDescriptor> _loadedPlugins = [];
    private readonly List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)> _pluginInstances = [];
    private readonly List<(IPluginComponent Component, string PluginId, Type ComponentType)> _pluginComponents = [];
    private readonly List<(string PluginId, string AssemblyLocation, IReadOnlyList<Type> EntityTypes)> _conventionalManagedEntityProviders = [];
    private readonly List<(string PluginId, Type ComponentType)> _conventionalApiComponents = [];
    private readonly HashSet<string> _mappedEndpointKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<PluginLoader> _logger;
    private readonly PluginScanner _scanner;
    private readonly PluginServiceRegistrar _serviceRegistrar;
    private readonly PluginDependencyResolver _dependencyResolver;
    private IEventBus? _eventBus;
    private IApplicationBuilder? _applicationBuilder;

    public IReadOnlyList<PluginDescriptor> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
        _scanner = new PluginScanner(logger);
        _serviceRegistrar = new PluginServiceRegistrar(logger);
        _dependencyResolver = new PluginDependencyResolver(logger);
    }

    public void SetEventBus(IEventBus eventBus) => _eventBus = eventBus;

    public IReadOnlyList<IAdminShellPlugin> GetPluginInstances()
        => _pluginInstances.Select(p => p.Instance).ToList().AsReadOnly();

    public IReadOnlyList<IPluginComponent> GetPluginComponents()
        => _pluginComponents.Select(p => p.Component).ToList().AsReadOnly();

    public IReadOnlyList<IManagedEntityProvider> GetManagedEntityProviders()
        => _conventionalManagedEntityProviders
            .Select(p => new ConventionalManagedEntityProvider(p.PluginId, p.EntityTypes))
            .ToList().AsReadOnly();

    public IReadOnlyList<IWidgetPlugin> GetWidgetPlugins()
        => _pluginComponents.Select(p => p.Component).OfType<IWidgetPlugin>().ToList().AsReadOnly();

    public IReadOnlyList<IMenuPlugin> GetMenuPlugins()
        => _pluginComponents.Select(p => p.Component).OfType<IMenuPlugin>().ToList().AsReadOnly();

    public async Task LoadPluginsAsync(string pluginsDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory not found: {Path}", pluginsDirectory);
            return;
        }

        _logger.LogInformation("Loading plugins from {Path}", pluginsDirectory);

        var assemblyFiles = GetCanonicalAssemblies(pluginsDirectory);
        _logger.LogInformation("Found {Count} canonical assemblies to scan", assemblyFiles.Count);

        foreach (var assemblyPath in assemblyFiles)
        {
            ct.ThrowIfCancellationRequested();
            await LoadPluginAsync(assemblyPath, ct);
        }

        DeduplicateAll();
        _dependencyResolver.Resolve(_pluginInstances);
        _logger.LogInformation("Loaded {Count} unique plugins", _loadedPlugins.Count);
    }

    public Task<PluginDescriptor?> LoadPluginAsync(string assemblyPath, CancellationToken ct = default)
    {
        try
        {
            var scanResult = _scanner.ScanAssembly(assemblyPath);

            if (scanResult.PluginTypes.Count == 0 && scanResult.ComponentTypes.Count == 0)
            {
                _logger.LogDebug("No plugin types found in {Assembly}", scanResult.Assembly.FullName);
                return Task.FromResult<PluginDescriptor?>(null);
            }

            PluginDescriptor? firstDescriptor = null;

            foreach (var pluginType in scanResult.PluginTypes)
            {
                var instance = (IAdminShellPlugin)Activator.CreateInstance(pluginType)!;
                var descriptor = _scanner.CreateDescriptor(instance, scanResult.AssemblyPath, scanResult.Assembly);

                _pluginInstances.Add((instance, descriptor));

                RegisterManagedEntityProviders(scanResult, instance.Id);
                RegisterApiComponents(scanResult, instance.Id);

                firstDescriptor ??= descriptor;
                _logger.LogInformation("Loaded plugin: {Name} v{Version}", instance.Name, instance.Version);
            }

            foreach (var componentType in scanResult.ComponentTypes)
            {
                var component = (IPluginComponent)Activator.CreateInstance(componentType)!;
                var pluginId = PluginComponentMetadata.GetPluginId(component);
                _pluginComponents.Add((component, pluginId, componentType));
                _logger.LogDebug("Loaded component {Component} for plugin {PluginId}",
                    componentType.FullName, pluginId);
            }

            return Task.FromResult(firstDescriptor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from {Path}", assemblyPath);
            return Task.FromResult<PluginDescriptor?>(new PluginDescriptor
            {
                Id = Path.GetFileNameWithoutExtension(assemblyPath),
                Name = Path.GetFileNameWithoutExtension(assemblyPath),
                Status = PluginStatus.Failed,
                ErrorMessage = ex.Message,
                LoadedAt = DateTime.UtcNow
            });
        }
    }

    public void InitializePlugins(IServiceCollection services, IConfiguration configuration)
        => InitializePlugins(services, configuration, null);

    public void InitializePlugins(IServiceCollection services, IConfiguration configuration, IQueryRegistry? queryRegistry = null)
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Status is not (PluginStatus.Loaded or PluginStatus.Active))
                continue;

            try
            {
                instance.Initialize(services, configuration);
                _serviceRegistrar.RegisterConventionalServices(services, instance.GetType().Assembly);

                if (queryRegistry is not null && instance is IDataPlugin dataPlugin)
                    dataPlugin.RegisterQueries(queryRegistry);

                UpdatePluginStatus(i, instance, descriptor with { Status = PluginStatus.Active });
                _logger.LogInformation("Initialized plugin: {Name}", instance.Name);
                _eventBus?.PublishAsync(new PluginStatusChangedEvent(instance.Id, instance.Name, "Active"));
            }
            catch (Exception ex)
            {
                UpdatePluginStatus(i, instance, descriptor with { Status = PluginStatus.Failed, ErrorMessage = ex.Message });
                _logger.LogError(ex, "Failed to initialize plugin: {Name}", instance.Name);
                _eventBus?.PublishAsync(new PluginStatusChangedEvent(instance.Id, instance.Name, "Failed"));
            }
        }
    }

    public void ConfigurePlugins(IApplicationBuilder app, IWebHostEnvironment env)
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Status is not (PluginStatus.Loaded or PluginStatus.Active))
                continue;

            try
            {
                instance.Configure(app, env);
                if (descriptor.Status == PluginStatus.Loaded)
                    _pluginInstances[i] = (instance, descriptor with { Status = PluginStatus.Active });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure plugin: {Name}", instance.Name);
            }
        }
    }

    public void MapPluginEndpoints(IApplicationBuilder app)
    {
        _applicationBuilder = app;
        if (app is not IEndpointRouteBuilder endpoints) return;

        foreach (var (component, pluginId, componentType) in _pluginComponents)
        {
            if (!IsPluginActive(pluginId) || component is not IApiPlugin apiPlugin)
                continue;

            var key = $"{pluginId}:{componentType.FullName}";
            if (_mappedEndpointKeys.Add(key))
            {
                try { apiPlugin.MapEndpoints(endpoints); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to map endpoints for component {Component} of plugin {PluginId}",
                        componentType.Name, pluginId);
                }
            }
        }

        foreach (var (pluginId, componentType) in _conventionalApiComponents)
        {
            if (!IsPluginActive(pluginId)) continue;

            var key = $"{pluginId}:{componentType.FullName}";
            if (_mappedEndpointKeys.Add(key))
            {
                try
                {
                    var apiPlugin = (IApiPlugin)endpoints.ServiceProvider.GetRequiredService(componentType);
                    apiPlugin.MapEndpoints(endpoints);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to map conventional endpoints for {Component} of plugin {PluginId}",
                        componentType.Name, pluginId);
                }
            }
        }
    }

    public void RefreshPluginEndpoints()
    {
        if (_applicationBuilder is not null)
            MapPluginEndpoints(_applicationBuilder);
    }

    public Task<bool> EnablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var idx = _loadedPlugins.FindIndex(p => p.Id == pluginId);
        if (idx < 0) return Task.FromResult(false);

        _loadedPlugins[idx] = _loadedPlugins[idx] with { Status = PluginStatus.Active };
        UpdateInstanceStatus(pluginId, PluginStatus.Active);
        _logger.LogInformation("Plugin enabled: {Id}", pluginId);
        _eventBus?.PublishAsync(new PluginStatusChangedEvent(pluginId, pluginId, "Active"));
        RefreshPluginEndpoints();
        return Task.FromResult(true);
    }

    public Task<bool> DisablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var idx = _loadedPlugins.FindIndex(p => p.Id == pluginId);
        if (idx < 0) return Task.FromResult(false);

        _loadedPlugins[idx] = _loadedPlugins[idx] with { Status = PluginStatus.Disabled };
        UpdateInstanceStatus(pluginId, PluginStatus.Disabled);
        _logger.LogInformation("Plugin disabled: {Id}", pluginId);
        _eventBus?.PublishAsync(new PluginStatusChangedEvent(pluginId, pluginId, "Disabled"));
        return Task.FromResult(true);
    }

    public EmbeddedFrontendManifest? GetEmbeddedFrontendManifest(string pluginId)
    {
        var active = GetActivePluginDescriptor(pluginId);
        if (active is null) return null;

        var assembly = Assembly.LoadFrom(active.Value.Descriptor.AssemblyPath);
        var manifest = _scanner.ReadEmbeddedFrontendManifest(assembly);
        return manifest is null ? null : manifest with { Id = string.IsNullOrWhiteSpace(manifest.Id) ? pluginId : manifest.Id, Source = "embedded" };
    }

    public EmbeddedFrontendAsset? GetEmbeddedFrontendAsset(string pluginId, string path)
    {
        var active = GetActivePluginDescriptor(pluginId);
        if (active is null) return null;

        var descriptor = active.Value.Descriptor;
        if (descriptor.FrontendManifestResourceName is null) return null;

        var normalizedPath = NormalizeEmbeddedFrontendPath(path);
        if (normalizedPath is null) return null;

        if (normalizedPath == "manifest.json")
        {
            var manifest = GetEmbeddedFrontendManifest(pluginId);
            if (manifest is null) return null;
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return new EmbeddedFrontendAsset(normalizedPath, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
        }

        var assembly = Assembly.LoadFrom(descriptor.AssemblyPath);
        var resourceName = BuildEmbeddedFrontendResourceName(descriptor, normalizedPath);
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return new EmbeddedFrontendAsset(normalizedPath, GetContentType(normalizedPath), memoryStream.ToArray());
    }

    // --- Private helpers ---

    private void UpdatePluginStatus(int index, IAdminShellPlugin instance, PluginDescriptor descriptor)
    {
        _pluginInstances[index] = (instance, descriptor);

        var loadedIdx = _loadedPlugins.FindIndex(p => p.Id == instance.Id);
        if (loadedIdx >= 0)
            _loadedPlugins[loadedIdx] = descriptor;
    }

    private void UpdateInstanceStatus(string pluginId, PluginStatus status)
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Id == pluginId)
                _pluginInstances[i] = (instance, descriptor with { Status = status });
        }
    }

    private (IAdminShellPlugin Instance, PluginDescriptor Descriptor)? GetActivePluginDescriptor(string pluginId)
        => _pluginInstances.FirstOrDefault(p =>
            p.Descriptor.Id == pluginId &&
            p.Descriptor.Status is PluginStatus.Active or PluginStatus.Loaded);

    private bool IsPluginActive(string pluginId)
        => _loadedPlugins.Any(p =>
            p.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase) &&
            p.Status == PluginStatus.Active);

    private void RegisterManagedEntityProviders(AssemblyScanResult scanResult, string pluginId)
    {
        if (scanResult.ManagedEntityTypes.Length == 0) return;
        _conventionalManagedEntityProviders.Add((pluginId, scanResult.AssemblyPath, scanResult.ManagedEntityTypes));
    }

    private void RegisterApiComponents(AssemblyScanResult scanResult, string pluginId)
    {
        foreach (var apiType in scanResult.ApiPluginTypes)
            _conventionalApiComponents.Add((pluginId, apiType));
    }

    private void DeduplicateAll()
    {
        foreach (var descriptor in _pluginInstances
                     .Select(p => p.Descriptor)
                     .Where(d => d.Status != PluginStatus.Failed))
        {
            if (_loadedPlugins.Any(p => p.Id == descriptor.Id)) continue;
            _loadedPlugins.Add(descriptor);
        }

        DeduplicateByKey(_pluginInstances, p => p.Descriptor.Id);
        DeduplicateByKey(_pluginComponents, c => $"{c.PluginId}:{c.ComponentType.FullName}");
        DeduplicateByKey(_conventionalApiComponents, c => $"{c.PluginId}:{c.ComponentType.FullName}");
        DeduplicateByKey(_conventionalManagedEntityProviders, p => $"{p.PluginId}:{p.AssemblyLocation}");
    }

    private static void DeduplicateByKey<T>(List<T> list, Func<T, string> keySelector)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (!seen.Add(keySelector(list[i])))
                list.RemoveAt(i);
        }
    }

    private static List<string> GetCanonicalAssemblies(string pluginsDirectory)
    {
        var allAssemblyFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories);
        var buildOutputAssemblyFiles = allAssemblyFiles
            .Where(IsBuildOutputAssembly)
            .ToArray();
        var assemblyFiles = buildOutputAssemblyFiles.Length > 0
            ? buildOutputAssemblyFiles
            : allAssemblyFiles;

        return assemblyFiles
            .GroupBy(asm => Path.GetFileNameWithoutExtension(asm))
            .Select(g => g
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenBy(p => p.Length)
                .ThenBy(p => p.Count(c => c == Path.DirectorySeparatorChar))
                .First())
            .ToList();
    }

    private static bool IsBuildOutputAssembly(string assemblyPath)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        var segments = directory
            .Replace('\\', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        return segments.Contains("bin") && !segments.Contains("ref") && !segments.Contains("refint");
    }

    private static string BuildEmbeddedFrontendResourceName(PluginDescriptor descriptor, string path)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(descriptor.AssemblyPath);
        var relativeName = path.Replace('\\', '.').Replace('/', '.');
        return $"{assemblyName}.frontend.{relativeName}";
    }

    private static string? NormalizeEmbeddedFrontendPath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s is "." or "..")) return null;
        return string.Join('/', segments);
    }

    private static string GetContentType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".css" => "text/css; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".map" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };

    private sealed class ConventionalManagedEntityProvider(string pluginId, IReadOnlyList<Type> entityTypes) : IManagedEntityProvider
    {
        public string PluginId { get; } = pluginId;
        public IEnumerable<Type> GetManagedEntityTypes() => entityTypes;
    }
}
