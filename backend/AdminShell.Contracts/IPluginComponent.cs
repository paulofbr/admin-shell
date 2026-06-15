using System.Reflection;

namespace AdminShell.Contracts;

/// <summary>
/// Marker interface for a contribution belonging to a plugin component.
/// </summary>
public interface IPluginComponent
{
}

/// <summary>
/// Metadata helpers for plugin components.
/// </summary>
public static class PluginComponentMetadata
{
    public static string GetPluginId(IPluginComponent component)
    {
        var attribute = component.GetType().GetCustomAttribute<PluginComponentAttribute>();

        if (attribute is null)
        {
            throw new InvalidOperationException(
                $"Plugin component '{component.GetType().FullName}' is missing [PluginComponent(...)].");
        }

        return attribute.PluginId;
    }
}
