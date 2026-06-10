namespace AdminShell.Contracts;

/// <summary>
/// Declares a dependency on another plugin.
/// Apply at the assembly level: [assembly: PluginDependency(typeof(SomePlugin), ">= 1.0.0")]
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class PluginDependencyAttribute : Attribute
{
    public Type PluginType { get; }
    public string? VersionConstraint { get; }

    public PluginDependencyAttribute(Type pluginType, string? versionConstraint = null)
    {
        PluginType = pluginType ?? throw new ArgumentNullException(nameof(pluginType));
        VersionConstraint = versionConstraint;
    }
}
