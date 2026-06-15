namespace AdminShell.Core.Entities;

public class PluginInfo : BaseEntity
{
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string AssemblyPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
    public string? SettingsJson { get; set; }
}
