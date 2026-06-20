using System.Reflection;
using System.Text.Json;
using AdminShell.Contracts;
using Microsoft.Extensions.Logging;

namespace AdminShell.Infrastructure.PluginSystem;

public class PluginScanner
{
    private readonly ILogger _logger;

    public PluginScanner(ILogger logger)
    {
        _logger = logger;
    }

    public AssemblyScanResult ScanAssembly(string assemblyPath)
    {
        LoadDependenciesFromDependenciaFolder(assemblyPath);

        var assembly = Assembly.LoadFrom(assemblyPath);
        var pluginTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IAdminShellPlugin).IsAssignableFrom(t) && !t.IsDefined(typeof(PluginComponentAttribute), inherit: true))
            .ToList();

        var componentTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsDefined(typeof(PluginComponentAttribute), inherit: true) && typeof(IPluginComponent).IsAssignableFrom(t))
            .ToList();

        var managedEntityTypes = assembly.GetTypes()
            .Where(IsManagedEntityType)
            .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var apiPluginTypes = assembly.GetTypes()
            .Where(IsConventionalApiPluginType)
            .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AssemblyScanResult
        {
            Assembly = assembly,
            AssemblyPath = assemblyPath,
            PluginTypes = pluginTypes,
            ComponentTypes = componentTypes,
            ManagedEntityTypes = managedEntityTypes,
            ApiPluginTypes = apiPluginTypes,
            Manifest = ReadEmbeddedFrontendManifest(assembly)
        };
    }

    public PluginDescriptor CreateDescriptor(IAdminShellPlugin instance, string assemblyPath, Assembly assembly)
    {
        return new PluginDescriptor
        {
            Id = instance.Id,
            Name = instance.Name,
            Version = instance.Version,
            Description = instance.Description,
            AssemblyPath = assemblyPath,
            Status = PluginStatus.Loaded,
            LoadedAt = DateTime.UtcNow,
            Dependencies = ReadDependencies(assembly),
            FrontendManifestResourceName = FindEmbeddedFrontendManifestResourceName(assembly)
        };
    }

    public EmbeddedFrontendManifest? ReadEmbeddedFrontendManifest(Assembly assembly)
    {
        var resourceName = FindEmbeddedFrontendManifestResourceName(assembly);
        if (resourceName is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return ParseManifestJson(reader.ReadToEnd());
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

    private IReadOnlyList<PluginDependencyInfo> ReadDependencies(Assembly assembly)
    {
        var manifest = ReadEmbeddedFrontendManifest(assembly);
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

    internal static EmbeddedFrontendManifest ParseManifestJson(string json)
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
            Dependencies = ReadManifestDependencies(root)
        };
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

    private static string[] ReadStyles(JsonElement root)
    {
        if (!root.TryGetProperty("styles", out var styles) || styles.ValueKind != JsonValueKind.Array)
            return [];

        return styles.EnumerateArray()
            .Select(style => style.GetString())
            .Where(style => !string.IsNullOrWhiteSpace(style))
            .ToArray()!;
    }

    private static PluginDependencyDescriptor[] ReadManifestDependencies(JsonElement root)
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

    private static bool IsManagedEntityType(Type type)
        => !type.IsAbstract && !type.IsInterface && type.IsDefined(typeof(ManagedEntityAttribute), inherit: true);

    private static bool IsConventionalApiPluginType(Type type)
        => !type.IsAbstract && !type.IsInterface
           && typeof(IApiPlugin).IsAssignableFrom(type)
           && !typeof(IAdminShellPlugin).IsAssignableFrom(type)
           && !type.IsDefined(typeof(PluginComponentAttribute), inherit: true)
           && (IsInPluginSubNamespace(type, "Apis") || type.Name.EndsWith("Api", StringComparison.OrdinalIgnoreCase));

    private static bool IsInPluginSubNamespace(Type type, string segment)
    {
        var ns = type.Namespace ?? string.Empty;
        return ns.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => part.Equals(segment, StringComparison.OrdinalIgnoreCase));
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
                .Any(a => string.Equals(Path.GetFileName(a.Location), dependencyName, StringComparison.OrdinalIgnoreCase));

            if (!alreadyLoaded)
            {
                Assembly.LoadFrom(dependency);
            }
        }
    }
}

public class AssemblyScanResult
{
    public required Assembly Assembly { get; init; }
    public required string AssemblyPath { get; init; }
    public required List<Type> PluginTypes { get; init; }
    public required List<Type> ComponentTypes { get; init; }
    public required Type[] ManagedEntityTypes { get; init; }
    public required Type[] ApiPluginTypes { get; init; }
    public EmbeddedFrontendManifest? Manifest { get; init; }
}
