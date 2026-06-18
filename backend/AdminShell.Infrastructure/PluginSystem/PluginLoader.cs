using System.Reflection;
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
    private readonly List<PluginDescriptor> _loadedPlugins = new();
    private readonly List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)> _pluginInstances = new();
    private readonly List<(IPluginComponent Component, string PluginId, Type ComponentType)> _pluginComponents = new();
    private readonly List<(string PluginId, string AssemblyLocation, IReadOnlyList<Type> EntityTypes)> _conventionalManagedEntityProviders = new();
    private readonly List<(string PluginId, Type ComponentType)> _conventionalApiComponents = new();
    private readonly HashSet<string> _mappedEndpointKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _initializedAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<PluginLoader> _logger;
    private IEventBus? _eventBus;
    private IApplicationBuilder? _applicationBuilder;

    public IReadOnlyList<PluginDescriptor> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public IReadOnlyList<IAdminShellPlugin> GetPluginInstances()
        => _pluginInstances.Select(p => p.Instance).ToList().AsReadOnly();

    public IReadOnlyList<IPluginComponent> GetPluginComponents()
        => _pluginComponents.Select(p => p.Component).ToList().AsReadOnly();

    public IReadOnlyList<IManagedEntityProvider> GetManagedEntityProviders()
        => _conventionalManagedEntityProviders
            .Select(provider => new ConventionalManagedEntityProvider(provider.PluginId, provider.EntityTypes))
            .ToList()
            .AsReadOnly();

    public IReadOnlyList<IWidgetPlugin> GetWidgetPlugins()
        => _pluginComponents.Select(p => p.Component).OfType<IWidgetPlugin>().ToList().AsReadOnly();

    public IReadOnlyList<IMenuPlugin> GetMenuPlugins()
        => _pluginComponents.Select(p => p.Component).OfType<IMenuPlugin>().ToList().AsReadOnly();

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

        var allAssemblyFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories);
        var buildOutputAssemblyFiles = allAssemblyFiles
            .Where(IsBuildOutputAssembly)
            .ToArray();
        var assemblyFiles = buildOutputAssemblyFiles.Length > 0
            ? buildOutputAssemblyFiles
            : allAssemblyFiles;
        var canonicalAssemblies = assemblyFiles
            .GroupBy(asm => Path.GetFileNameWithoutExtension(asm))
            .Select(g => g
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenBy(p => p.Length)
                .ThenBy(p => p.Count(c => c == Path.DirectorySeparatorChar))
                .First())
            .ToList();

        _logger.LogInformation("Found {Total} DLLs, will load {Canonical} unique assemblies",
            assemblyFiles.Length, canonicalAssemblies.Count);

        var results = new List<PluginDescriptor?>();
        foreach (var assemblyPath in canonicalAssemblies)
        {
            var descriptor = await LoadPluginAsync(assemblyPath, ct);
            results.Add(descriptor);
        }

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

        DeduplicatePluginComponents();
        DeduplicateConventionalManagedEntityProviders();
        DeduplicateConventionalApiComponents();

        ResolveDependencies();
        _logger.LogInformation("Loaded {Count} unique plugins", _loadedPlugins.Count);
    }

    public Task<PluginDescriptor?> LoadPluginAsync(string assemblyPath, CancellationToken ct = default)
    {
        try
        {
            LoadDependenciesFromDependenciaFolder(assemblyPath);

            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IAdminShellPlugin).IsAssignableFrom(t) && !t.IsDefined(typeof(PluginComponentAttribute), inherit: true))
                .ToList();

            var componentTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.IsDefined(typeof(PluginComponentAttribute), inherit: true) && typeof(IPluginComponent).IsAssignableFrom(t))
                .ToList();

            if (pluginTypes.Count == 0)
            {
                _logger.LogDebug("No plugin identities found in {Assembly}", assembly.FullName);
            }
            else
            {
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
                        Dependencies = GetDependencies(assembly),
                        FrontendManifestResourceName = FindEmbeddedFrontendManifestResourceName(assembly)
                    };

                    _pluginInstances.Add((instance, descriptor));

                    RegisterConventionalManagedEntityProviders(assembly, instance.Id);
                    RegisterConventionalApiPlugins(assembly, instance.Id);

                    descriptors.Add(descriptor);
                    _logger.LogInformation("Loaded plugin identity: {Name} v{Version}", instance.Name, instance.Version);
                }

                foreach (var componentType in componentTypes)
                {
                    var component = (IPluginComponent)Activator.CreateInstance(componentType)!;
                    var pluginId = PluginComponentMetadata.GetPluginId(component);
                    _pluginComponents.Add((component, pluginId, componentType));
                    _logger.LogDebug("Loaded plugin component {ComponentType} for plugin {PluginId}",
                        componentType.FullName,
                        pluginId);
                }

                return Task.FromResult<PluginDescriptor?>(descriptors.FirstOrDefault());
            }

            if (componentTypes.Count > 0)
            {
                foreach (var componentType in componentTypes)
                {
                    var component = (IPluginComponent)Activator.CreateInstance(componentType)!;
                    var pluginId = PluginComponentMetadata.GetPluginId(component);
                    _pluginComponents.Add((component, pluginId, componentType));
                    _logger.LogDebug("Loaded standalone plugin component {ComponentType} for plugin {PluginId}",
                        componentType.FullName,
                        pluginId);
                }

                return Task.FromResult<PluginDescriptor?>(null);
            }

            _logger.LogDebug("No plugins or plugin components found in {Assembly}", assembly.FullName);
            return Task.FromResult<PluginDescriptor?>(null);
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
        InitializePlugins(services, configuration, null);
    }

    public void InitializePlugins(
        IServiceCollection services,
        IConfiguration configuration,
        IQueryRegistry? queryRegistry = null)
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Status == PluginStatus.Loaded || descriptor.Status == PluginStatus.Active)
            {
                try
                {
                    instance.Initialize(services, configuration);
                    RegisterConventionalServices(services, instance.GetType().Assembly);

                    if (queryRegistry is not null && instance is IDataPlugin dataPlugin)
                    {
                        dataPlugin.RegisterQueries(queryRegistry);
                    }

                    var updated = descriptor with { Status = PluginStatus.Active };
                    _pluginInstances[i] = (instance, updated);

                    var loadedIndex = _loadedPlugins.FindIndex(p => p.Id == instance.Id);
                    if (loadedIndex >= 0)
                    {
                        _loadedPlugins[loadedIndex] = _loadedPlugins[loadedIndex] with { Status = PluginStatus.Active };
                    }

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
        _applicationBuilder = app;

        if (app is not IEndpointRouteBuilder endpoints) return;

        foreach (var (component, pluginId, componentType) in _pluginComponents)
        {
            if (!IsPluginActive(pluginId)) continue;

            if (component is not IApiPlugin apiPlugin) continue;

            var endpointKey = $"{pluginId}:{componentType.FullName}";
            if (_mappedEndpointKeys.Contains(endpointKey)) continue;

            try
            {
                apiPlugin.MapEndpoints(endpoints);
                _mappedEndpointKeys.Add(endpointKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map endpoints for plugin component {ComponentType} of plugin {PluginId}",
                    componentType.Name,
                    pluginId);
            }
        }

        foreach (var (pluginId, componentType) in _conventionalApiComponents)
        {
            if (!IsPluginActive(pluginId)) continue;

            var endpointKey = $"{pluginId}:{componentType.FullName}";
            if (_mappedEndpointKeys.Contains(endpointKey)) continue;

            try
            {
                var apiPlugin = (IApiPlugin)endpoints.ServiceProvider.GetRequiredService(componentType);
                apiPlugin.MapEndpoints(endpoints);
                _mappedEndpointKeys.Add(endpointKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map conventional API endpoints for plugin component {ComponentType} of plugin {PluginId}",
                    componentType.Name,
                    pluginId);
            }
        }
    }

    public void RefreshPluginEndpoints()
    {
        if (_applicationBuilder is null) return;
        MapPluginEndpoints(_applicationBuilder);
    }

    public Task<bool> EnablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var idx = _loadedPlugins.FindIndex(p => p.Id == pluginId);
        if (idx < 0) return Task.FromResult(false);

        _loadedPlugins[idx] = _loadedPlugins[idx] with { Status = PluginStatus.Active };
        UpdatePluginInstanceStatus(pluginId, PluginStatus.Active);
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
        UpdatePluginInstanceStatus(pluginId, PluginStatus.Disabled);
        _logger.LogInformation("Plugin disabled: {Id}", pluginId);
        _eventBus?.PublishAsync(new PluginStatusChangedEvent(pluginId, pluginId, "Disabled"));
        return Task.FromResult(true);
    }

    private void UpdatePluginInstanceStatus(string pluginId, PluginStatus status)
    {
        for (var i = 0; i < _pluginInstances.Count; i++)
        {
            var (instance, descriptor) = _pluginInstances[i];
            if (descriptor.Id != pluginId) continue;

            _pluginInstances[i] = (instance, descriptor with { Status = status });
        }
    }

    private void RegisterConventionalManagedEntityProviders(Assembly assembly, string pluginId)
    {
        var managedEntityTypes = assembly.GetTypes()
            .Where(IsManagedEntityType)
            .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (managedEntityTypes.Length == 0)
            return;

        _conventionalManagedEntityProviders.Add((pluginId, assembly.Location, managedEntityTypes));
        _logger.LogDebug("Registered {Count} conventional managed entity types for plugin {PluginId}",
            managedEntityTypes.Length,
            pluginId);
    }

    private void RegisterConventionalApiPlugins(Assembly assembly, string pluginId)
    {
        foreach (var apiType in assembly.GetTypes()
                     .Where(IsConventionalApiPluginType)
                     .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase))
        {
            _conventionalApiComponents.Add((pluginId, apiType));
            _logger.LogDebug("Registered conventional API plugin type {ComponentType} for plugin {PluginId}",
                apiType.FullName,
                pluginId);
        }
    }

    private void RegisterConventionalServices(IServiceCollection services, Assembly assembly)
    {
        var key = assembly.Location;
        if (!_initializedAssemblies.Add(key))
            return;

        foreach (var implementationType in assembly.GetTypes()
                     .Where(IsConventionalServiceOrRepositoryType)
                     .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase))
        {
            if (typeof(IApiPlugin).IsAssignableFrom(implementationType))
            {
                services.AddScoped(implementationType);
                _logger.LogDebug("Registered conventional API plugin type {ImplementationType}",
                    implementationType.FullName);
            }

            foreach (var serviceType in GetConventionalServiceInterfaces(implementationType)
                         .Where(serviceType => serviceType != typeof(IApiPlugin) && serviceType != typeof(IPluginComponent)))
            {
                try
                {
                    services.AddScoped(serviceType, implementationType);
                    _logger.LogDebug("Registered conventional plugin service {ServiceType} -> {ImplementationType}",
                        serviceType.FullName,
                        implementationType.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to register conventional plugin service {ServiceType} -> {ImplementationType}",
                        serviceType.FullName,
                        implementationType.FullName);
                }
            }
        }
    }

    private static bool IsConventionalApiPluginType(Type type)
        => !type.IsAbstract
           && !type.IsInterface
           && typeof(IApiPlugin).IsAssignableFrom(type)
           && !typeof(IAdminShellPlugin).IsAssignableFrom(type)
           && !type.IsDefined(typeof(PluginComponentAttribute), inherit: true)
           && (IsInPluginSubNamespace(type, "Apis") || type.Name.EndsWith("Api", StringComparison.OrdinalIgnoreCase));

    private static bool IsManagedEntityType(Type type)
        => !type.IsAbstract
           && !type.IsInterface
           && type.IsDefined(typeof(ManagedEntityAttribute), inherit: true);

    private static bool IsConventionalServiceOrRepositoryType(Type type)
        => !type.IsAbstract
           && !type.IsInterface
           && !typeof(IAdminShellPlugin).IsAssignableFrom(type)
           && (IsInPluginSubNamespace(type, "Services")
               || IsInPluginSubNamespace(type, "Repositories")
               || IsInPluginSubNamespace(type, "Apis")
               || type.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase)
               || type.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase)
               || type.Name.EndsWith("Api", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Type> GetConventionalServiceInterfaces(Type implementationType)
    {
        var namespacePrefix = implementationType.Namespace ?? string.Empty;
        return implementationType.GetInterfaces()
            .Where(serviceType => serviceType.IsPublic
                                  && !serviceType.IsGenericTypeDefinition
                                  && serviceType.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) == true)
            .DefaultIfEmpty(implementationType)
            .Distinct()
            .OrderBy(serviceType => serviceType.FullName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsInPluginSubNamespace(Type type, string segment)
    {
        var ns = type.Namespace ?? string.Empty;
        return ns.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => part.Equals(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPublicParameterlessConstructor(Type type)
        => type.GetConstructor(Type.EmptyTypes) is not null;

    private void DeduplicatePluginComponents()
    {
        var activePluginIds = _pluginInstances
            .Select(p => p.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = _pluginComponents.Count - 1; i >= 0; i--)
        {
            var (component, pluginId, componentType) = _pluginComponents[i];
            var componentKey = $"{pluginId}:{componentType.FullName}";

            if (!activePluginIds.Contains(pluginId) || !seenComponents.Add(componentKey))
            {
                _pluginComponents.RemoveAt(i);
            }
        }
    }

    private void DeduplicateConventionalApiComponents()
    {
        var activePluginIds = _pluginInstances
            .Select(p => p.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = _conventionalApiComponents.Count - 1; i >= 0; i--)
        {
            var (pluginId, componentType) = _conventionalApiComponents[i];
            var componentKey = $"{pluginId}:{componentType.FullName}";

            if (!activePluginIds.Contains(pluginId) || !seenComponents.Add(componentKey))
            {
                _conventionalApiComponents.RemoveAt(i);
            }
        }
    }

    private void DeduplicateConventionalManagedEntityProviders()
    {
        var activePluginIds = _pluginInstances
            .Select(p => p.Descriptor.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = _conventionalManagedEntityProviders.Count - 1; i >= 0; i--)
        {
            var (pluginId, assemblyLocation, _) = _conventionalManagedEntityProviders[i];
            var providerKey = $"{pluginId}:{assemblyLocation}";

            if (!activePluginIds.Contains(pluginId) || !seenProviders.Add(providerKey))
            {
                _conventionalManagedEntityProviders.RemoveAt(i);
            }
        }
    }

    private bool IsPluginActive(string pluginId)
    {
        return _loadedPlugins.Any(p =>
            p.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase) &&
            p.Status == PluginStatus.Active);
    }

    public EmbeddedFrontendManifest? GetEmbeddedFrontendManifest(string pluginId)
    {
        var activePlugin = GetActivePluginDescriptor(pluginId);
        if (!activePlugin.HasValue)
            return null;

        var descriptor = activePlugin.Value.Descriptor;
        if (descriptor.FrontendManifestResourceName is null)
            return null;

        var assembly = Assembly.LoadFrom(descriptor.AssemblyPath);
        using var stream = assembly.GetManifestResourceStream(descriptor.FrontendManifestResourceName)
            ?? throw new InvalidOperationException($"Embedded frontend manifest not found for plugin {pluginId}");
        var manifest = ReadEmbeddedFrontendManifestFromAssembly(assembly);

        if (manifest is null)
            return null;

        return manifest with
        {
            Id = string.IsNullOrWhiteSpace(manifest.Id) ? pluginId : manifest.Id,
            Source = "embedded"
        };
    }

    public EmbeddedFrontendAsset? GetEmbeddedFrontendAsset(string pluginId, string path)
    {
        var activePlugin = GetActivePluginDescriptor(pluginId);
        if (!activePlugin.HasValue)
            return null;

        var descriptor = activePlugin.Value.Descriptor;
        if (descriptor.FrontendManifestResourceName is null)
            return null;

        var normalizedPath = NormalizeEmbeddedFrontendPath(path);
        if (normalizedPath is null)
            return null;

        if (normalizedPath == "manifest.json")
        {
            var manifest = GetEmbeddedFrontendManifest(pluginId);
            if (manifest is null)
                return null;

            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return new EmbeddedFrontendAsset(
                normalizedPath,
                "application/json; charset=utf-8",
                System.Text.Encoding.UTF8.GetBytes(json));
        }

        var assembly = Assembly.LoadFrom(descriptor.AssemblyPath);
        var resourceName = BuildEmbeddedFrontendResourceName(descriptor, normalizedPath);
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return new EmbeddedFrontendAsset(normalizedPath, GetContentType(normalizedPath), memoryStream.ToArray());
    }

    private (IAdminShellPlugin Instance, PluginDescriptor Descriptor)? GetActivePluginDescriptor(string pluginId)
    {
        return _pluginInstances.FirstOrDefault(p =>
            p.Descriptor.Id == pluginId &&
            (p.Descriptor.Status == PluginStatus.Active || p.Descriptor.Status == PluginStatus.Loaded));
    }

    private static string? FindEmbeddedFrontendManifestResourceName(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
            return null;

        var expectedSuffix = $"{assemblyName}.plugin.json";
        return assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase));
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
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".."))
            return null;

        return string.Join('/', segments);
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
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
    }

    private EmbeddedFrontendManifest? ReadEmbeddedFrontendManifest(Assembly assembly)
    {
        var embeddedManifest = ReadEmbeddedFrontendManifestFromAssembly(assembly);
        if (embeddedManifest is not null)
            return embeddedManifest;

        var manifestPath = FindManifestPath(Path.GetDirectoryName(assembly.Location));
        return manifestPath is null ? null : ReadManifestFile(manifestPath);
    }

    private EmbeddedFrontendManifest? ReadEmbeddedFrontendManifestFromAssembly(Assembly assembly)
    {
        var resourceName = FindEmbeddedFrontendManifestResourceName(assembly);
        if (resourceName is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return ReadManifestJson(reader.ReadToEnd());
    }

    private static string? FindManifestPath(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            return null;

        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "manifest.json");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return null;
    }

    private static EmbeddedFrontendManifest ReadManifestFile(string manifestPath)
    {
        return ReadManifestJson(File.ReadAllText(manifestPath));
    }

    private static EmbeddedFrontendManifest ReadManifestJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var schemaVersion = root.TryGetProperty("schemaVersion", out var schemaVersionElement)
            ? schemaVersionElement.TryGetInt32(out var value) ? value : 1
            : 1;
        if (schemaVersion != 1)
            throw new JsonException("Unsupported plugin manifest schemaVersion. Expected 1.");

        return new EmbeddedFrontendManifest
        {
            SchemaVersion = schemaVersion,
            Id = RequireString(root, "id"),
            Name = RequireString(root, "name"),
            Version = RequireString(root, "version"),
            Description = RequireString(root, "description"),
            Main = root.TryGetProperty("main", out var main) ? main.GetString() ?? "index.js" : "index.js",
            Styles = ReadStyles(root),
            Dependencies = ReadDependencies(root)
        };
    }

    private static string[] ReadStyles(JsonElement root)
    {
        if (!root.TryGetProperty("styles", out var styles) || styles.ValueKind != JsonValueKind.Array)
            return [];

        return styles.EnumerateArray()
            .Select(style => style.GetString())
            .Where(style => !string.IsNullOrWhiteSpace(style))
            .ToArray()!;
    }

    private static string RequireString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            throw new JsonException($"Plugin manifest is missing required property '{propertyName}'.");

        var value = property.ValueKind == JsonValueKind.String ? property.GetString() : null;
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException($"Plugin manifest property '{propertyName}' must be a non-empty string.");

        return value;
    }

    private static PluginDependencyDescriptor[] ReadDependencies(JsonElement root)
    {
        if (!root.TryGetProperty("dependencies", out var dependencies))
            return [];

        if (dependencies.ValueKind != JsonValueKind.Array)
            throw new JsonException("Plugin manifest dependencies must be an array of objects with id and version.");

        return dependencies.EnumerateArray()
            .Select(dep => new PluginDependencyDescriptor
            {
                Id = RequireString(dep, "id"),
                Version = RequireString(dep, "version")
            })
            .Where(dep => !string.IsNullOrWhiteSpace(dep.Id))
            .ToArray();
    }

    private IReadOnlyList<PluginDependencyInfo> GetDependencies(Assembly assembly)
    {
        var manifest = ReadManifestFileFromAssemblyLocation(assembly) ?? ReadEmbeddedFrontendManifest(assembly);
        if (manifest is null)
            return Array.Empty<PluginDependencyInfo>();

        return manifest.Dependencies
            .Select(dep => new PluginDependencyInfo
            {
                PluginId = dep.Id,
                VersionConstraint = dep.Version,
                Version = dep.Version,
                IsOptional = false,
                IsResolved = false,
                ErrorMessage = null
            })
            .ToList();
    }

    private static EmbeddedFrontendManifest? ReadManifestFileFromAssemblyLocation(Assembly assembly)
    {
        var manifestPath = FindManifestPath(Path.GetDirectoryName(assembly.Location));
        return manifestPath is null ? null : ReadManifestFile(manifestPath);
    }

    private void ResolveDependencies()
    {
        var available = _pluginInstances
            .GroupBy(p => p.Descriptor.Id)
            .ToDictionary(g => g.Key, g => g.First().Descriptor.Version);

        var resolved = new HashSet<string>();
        var ordered = new List<(IAdminShellPlugin Instance, PluginDescriptor Descriptor)>();

        void Visit((IAdminShellPlugin Instance, PluginDescriptor Descriptor) item)
        {
            if (resolved.Contains(item.Descriptor.Id)) return;
            resolved.Add(item.Descriptor.Id);

            var descriptor = item.Descriptor;
            var dependencies = descriptor.Dependencies.ToList();

            for (var i = 0; i < dependencies.Count; i++)
            {
                var dep = dependencies[i];

                if (available.TryGetValue(dep.PluginId, out var availableVersion))
                {
                    var isResolved = IsVersionSatisfied(availableVersion, dep.VersionConstraint);
                    dep = dep with
                    {
                        VersionConstraint = dep.VersionConstraint,
                        Version = dep.Version ?? dep.VersionConstraint,
                        IsResolved = isResolved,
                        ErrorMessage = isResolved
                            ? null
                            : $"Dependency {dep.PluginId} version {availableVersion} does not satisfy {dep.VersionConstraint}"
                    };

                    if (!isResolved)
                    {
                        descriptor = descriptor with
                        {
                            Status = PluginStatus.Failed,
                            ErrorMessage = dep.ErrorMessage
                        };
                    }
                }
                else
                {
                    dep = dep with
                    {
                        VersionConstraint = dep.VersionConstraint,
                        Version = dep.Version ?? dep.VersionConstraint,
                        IsResolved = dep.IsOptional,
                        ErrorMessage = dep.IsOptional
                            ? $"Optional dependency {dep.PluginId} is not installed"
                            : $"Missing required dependency {dep.PluginId}"
                    };

                    if (!dep.IsOptional)
                    {
                        descriptor = descriptor with
                        {
                            Status = PluginStatus.Failed,
                            ErrorMessage = dep.ErrorMessage
                        };
                    }
                }

                dependencies[i] = dep;

                var depItem = _pluginInstances.FirstOrDefault(p => p.Descriptor.Id == dep.PluginId);
                if (depItem.Instance != null && dep.IsResolved)
                {
                    Visit(depItem);
                }
            }

            descriptor = descriptor with { Dependencies = dependencies };
            ordered.Add((item.Instance, descriptor));
        }

        foreach (var item in _pluginInstances)
        {
            Visit(item);
        }

        _pluginInstances.Clear();
        _pluginInstances.AddRange(ordered);
    }

    private static bool IsBuildOutputAssembly(string assemblyPath)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        var segments = directory
            .Replace('\\', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        return segments.Contains("bin") &&
               !segments.Contains("ref") &&
               !segments.Contains("refint");
    }

    private static void LoadDependenciesFromDependenciaFolder(string assemblyPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        if (string.IsNullOrWhiteSpace(assemblyDirectory)) return;

        var dependenciesDirectory = Path.Combine(assemblyDirectory, "dependencias");
        if (!Directory.Exists(dependenciesDirectory)) return;

        foreach (var dependency in Directory.GetFiles(dependenciesDirectory, "*.dll"))
        {
            var dependencyName = Path.GetFileName(dependency);
            var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => string.Equals(Path.GetFileName(assembly.Location), dependencyName, StringComparison.OrdinalIgnoreCase));

            if (!alreadyLoaded)
            {
                Assembly.LoadFrom(dependency);
            }
        }
    }

    private sealed class ConventionalManagedEntityProvider : IManagedEntityProvider
    {
        private readonly IReadOnlyList<Type> _entityTypes;

        public ConventionalManagedEntityProvider(string pluginId, IReadOnlyList<Type> entityTypes)
        {
            PluginId = pluginId;
            _entityTypes = entityTypes;
        }

        public string PluginId { get; }

        public IEnumerable<Type> GetManagedEntityTypes() => _entityTypes;
    }

    private static bool IsVersionSatisfied(string installedVersion, string? constraint)
    {
        if (string.IsNullOrWhiteSpace(constraint) || constraint.Trim() == "*")
            return true;

        var trimmed = constraint.Trim();
        var op = ">=";
        var required = trimmed;

        foreach (var candidate in new[] { ">=", "<=", "!=", ">", "<", "=" })
        {
            if (trimmed.StartsWith(candidate, StringComparison.Ordinal))
            {
                op = candidate;
                required = trimmed[candidate.Length..].Trim();
                break;
            }
        }

        if (!Version.TryParse(installedVersion, out var installed) ||
            !Version.TryParse(required, out var target))
        {
            return string.Equals(installedVersion, required, StringComparison.OrdinalIgnoreCase);
        }

        return op switch
        {
            ">=" => installed >= target,
            "<=" => installed <= target,
            ">" => installed > target,
            "<" => installed < target,
            "!=" => installed != target,
            _ => installed == target,
        };
    }
}