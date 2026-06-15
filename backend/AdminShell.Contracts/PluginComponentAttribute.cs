namespace AdminShell.Contracts;

/// <summary>
/// Identifies the plugin that owns this component.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginComponentAttribute : Attribute
{
    public PluginComponentAttribute(string pluginId)
    {
        PluginId = pluginId;
    }

    public string PluginId { get; }
}
