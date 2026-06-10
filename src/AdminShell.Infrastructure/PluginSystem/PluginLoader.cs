using System.Reflection;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.PluginSystem;

public class PluginLoader : IPluginLoader
{
    private readonly List<PluginDescriptor> _loadedPlugins = new();
    private readonly List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)> _pluginInstances = new();
    private readonly ILogger<PluginLoader> _logger;
    private List<IWidgetPlugin> _widgetPlugins = new();
    private List<IMenuPlugin> _menuPlugins = new();
    private IEventBus? _eventBus;

    public IReadOnlyList<PluginDescriptor> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public IReadOnlyList<IAdminShellPlugin> GetPluginInstances()
        => _pluginInstances.Select(p => p.Instance).ToList().AsReadOnly();

    public IReadOnlyList<IWidgetPlugin> GetWidgetPlugins() => _widgetPlugins.AsReadOnly();
    public IReadOnlyList<IMenuPlugin> GetMenuPlugins() => _menuPlugins.AsReadOnly();

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    public void SetEventBus(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task LoadPluginsAsync(string pluginsDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory not found: {Path}", pluginsDirectory);
            return;
        }

        _logger.LogInformation("Loading plugins from {Path}", pluginsDirectory);

        var assemblyFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories);
        var canonicalAssemblies = assemblyFiles
            .GroupBy(asm => Path.GetFileNameWithoutExtension(asm))
            .Select(g => g.OrderBy(p => p.Length).ThenBy(p => p.Count(c => c == Path.DirectorySeparatorChar)).First())
            .ToList();

        _logger.LogInformation("Found {Total} DLLs, will load {Canonical} unique assemblies",
            assemblyFiles.Length, canonicalAssemblies.Count);

        var loadTasks = canonicalAssemblies.Select(asm => LoadPluginAsync(asm, ct));
        var results = await Task.WhenAll(loadTasks);

        foreach (var descriptor in results.Where(d => d is not null).Cast<PluginDescriptor>())
        {
            if (_loadedPlugins.Any(p => p.Id == descriptor.Id))
            {
                _logger.LogDebug("Skipping duplicate plugin {Name} (Id={Id}) from {Path}",
                    descriptor.Name, descriptor.Id, descriptor.AssemblyPath);
                continue;
            }

            if (descriptor.Status == PluginStatus.Failed)
            {
                _logger.LogDebug("Skipping failed duplicate plugin {Id} from {Path}",
                    descriptor.Id, descriptor.AssemblyPath);
                continue;
            }

            _loadedPlugins.Add(descriptor);
        }

        // Deduplicate plugin instances by ID (keep first)
        var seenIds = new HashSet<string>();
        for (var i = _pluginInstances.Count - 1; i >= 0; i--)
        {
            if (!seenIds.Add(_pluginInstances[i].Descriptor.Id))
            {
                _pluginInstances.RemoveAt(i);
            }
        }

        ResolveDependencies();
        _logger.LogInformation("Loaded {Count} unique plugins", _loadedPlugins.Count);
    }

    public Task<PluginDescriptor?> LoadPluginAsync(string assemblyPath, CancellationToken ct = default)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IAdminShellPlugin).IsAssignableFrom(t))
                .ToList();

            if (pluginTypes.Count == 0)
            {
                _logger.LogDebug("No plugins found in {Assembly}", assembly.FullName);
                return Task.FromResult<PluginDescriptor?>(null);
            }

            var descriptors = new List<PluginDescriptor>();

            foreach (var pluginType in pluginTypes)
            {
                var instance = (IAdminShellPlugin)Activator.CreateInstance(pluginType)!;
                var descriptor = new PluginDescriptor
                {
                    Id = instance.Id,
                    Name = instance.Name,
                    Version = instance.Version,
                    Description = instance.Description,
                    AssemblyPath = assemblyPath,
                    Status = PluginStatus.Loaded,
                    LoadedAt = DateTime.UtcNow,
                    Dependencies = GetDependencies(assembly)
                };

                _pluginInstances.Add((instance, descriptor));

                if (instance is IWidgetPlugin) _widgetPlugins.Add((IWidgetPlugin)instance);
                if (instance is IMenuPlugin) _menuPlugins.Add((IMenuPlugin)instance);
                descriptors.Add(descriptor);
                _logger.LogInformation("Loaded plugin: {Name} v{Version}", instance.Name, instance.Version);
            }

            return Task.FromResult<PluginDescriptor?>(descriptors.FirstOrDefault());
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
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Status == PluginStatus.Loaded || descriptor.Status == PluginStatus.Active)
            {
                try
                {
                    instance.Initialize(services, configuration);
                    var updated = descriptor with { Status = PluginStatus.Active };
                    _pluginInstances[i] = (instance, updated);
                    _logger.LogInformation("Initialized plugin: {Name}", instance.Name);
                    _eventBus?.PublishAsync(new PluginStatusChangedEvent(instance.Id, instance.Name, "Active"));
                }
                catch (Exception ex)
                {
                    var updated = descriptor with { Status = PluginStatus.Failed, ErrorMessage = ex.Message };
                    _pluginInstances[i] = (instance, updated);
                    _logger.LogError(ex, "Failed to initialize plugin: {Name}", instance.Name);
                    _eventBus?.PublishAsync(new PluginStatusChangedEvent(instance.Id, instance.Name, "Failed"));
                }
            }
        }
    }

    public void ConfigurePlugins(IApplicationBuilder app, IWebHostEnvironment env)
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Status == PluginStatus.Loaded || descriptor.Status == PluginStatus.Active)
            {
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
    }

    public void MapPluginEndpoints(IApplicationBuilder app)
    {
        if (app is not IEndpointRouteBuilder endpoints) return;

        foreach (var (instance, descriptor) in _pluginInstances)
        {
            if (descriptor.Status == PluginStatus.Active && instance is IApiPlugin apiPlugin)
            {
                try
                {
                    apiPlugin.MapEndpoints(endpoints);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to map endpoints for plugin: {Name}", instance.Name);
                }
            }
        }
    }

    public Task<bool> EnablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var idx = _loadedPlugins.FindIndex(p => p.Id == pluginId);
        if (idx < 0) return Task.FromResult(false);

        _loadedPlugins[idx] = _loadedPlugins[idx] with { Status = PluginStatus.Active };
        _logger.LogInformation("Plugin enabled: {Id}", pluginId);
        _eventBus?.PublishAsync(new PluginStatusChangedEvent(pluginId, pluginId, "Active"));
        return Task.FromResult(true);
    }

    public Task<bool> DisablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var idx = _loadedPlugins.FindIndex(p => p.Id == pluginId);
        if (idx < 0) return Task.FromResult(false);

        _loadedPlugins[idx] = _loadedPlugins[idx] with { Status = PluginStatus.Disabled };
        _logger.LogInformation("Plugin disabled: {Id}", pluginId);
        _eventBus?.PublishAsync(new PluginStatusChangedEvent(pluginId, pluginId, "Disabled"));
        return Task.FromResult(true);
    }

    private IReadOnlyList<PluginDependencyInfo> GetDependencies(Assembly assembly)
    {
        var deps = new List<PluginDependencyInfo>();
        var attributes = assembly.GetCustomAttributes<PluginDependencyAttribute>();

        foreach (var attr in attributes)
        {
            deps.Add(new PluginDependencyInfo
            {
                PluginId = attr.PluginType.Name.Replace("Plugin", ""),
                VersionConstraint = attr.VersionConstraint
            });
        }

        return deps;
    }

    private void ResolveDependencies()
    {
        var resolved = new HashSet<string>();
        var ordered = new List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)>();

        void Visit((IAdminShellPlugin Instance, PluginDescriptor Descriptor) item)
        {
            if (resolved.Contains(item.Descriptor.Id)) return;
            resolved.Add(item.Descriptor.Id);

            foreach (var dep in item.Descriptor.Dependencies)
            {
                var depItem = _pluginInstances.FirstOrDefault(p => p.Descriptor.Id == dep.PluginId);
                if (depItem.Instance != null)
                {
                    Visit(depItem);
                }
            }

            ordered.Add(item);
        }

        foreach (var item in _pluginInstances)
        {
            Visit(item);
        }

        _pluginInstances.Clear();
        _pluginInstances.AddRange(ordered);
    }
}