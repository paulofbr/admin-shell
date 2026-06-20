using System.Collections.Concurrent;
using System.Reflection;
using AdminShell.Contracts;

namespace AdminShell.Infrastructure.PluginSystem;

public sealed class PermissionDefinitionRegistry : IPermissionDefinitionRegistry
{
    private readonly ConcurrentDictionary<string, PermissionDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PermissionDefinition> GetAll()
        => _definitions.Values
            .OrderBy(permission => permission.PluginId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(permission => permission.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(permission => permission.Name, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

    public IReadOnlyList<PermissionDefinition> GetByPlugin(string pluginId)
        => GetAll()
            .Where(permission => permission.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

    public PermissionDefinition? GetByCode(string code)
        => _definitions.GetValueOrDefault(code);

    public void Discover(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            DiscoverAssembly(assembly);
        }
    }

    public void DiscoverAssembly(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        foreach (var type in types.Where(IsPermissionDefinitionProvider))
        {
            RegisterProvider(type);
        }
    }

    private static bool IsPermissionDefinitionProvider(Type type)
        => type is { IsAbstract: false, IsInterface: false }
           && type.IsDefined(typeof(PermissionDefinitionsAttribute), inherit: true);

    private void RegisterProvider(Type providerType)
    {
        var providerAttribute = providerType.GetCustomAttribute<PermissionDefinitionsAttribute>()
            ?? throw new InvalidOperationException($"Permission provider '{providerType.FullName}' is missing PermissionDefinitionsAttribute metadata.");

        foreach (var field in providerType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (field.FieldType != typeof(string))
                continue;

            var definitionAttribute = field.GetCustomAttribute<PermissionDefinitionAttribute>();
            if (definitionAttribute is null)
                continue;

            var code = field.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(code))
                continue;

            _definitions[code] = new PermissionDefinition(
                providerAttribute.PluginId,
                code,
                definitionAttribute.Group,
                definitionAttribute.Name);
        }
    }
}
