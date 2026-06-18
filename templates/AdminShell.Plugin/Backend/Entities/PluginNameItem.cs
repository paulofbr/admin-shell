using AdminShell.Contracts;

namespace PluginName.Entities;

[ManagedEntity]
public sealed class PluginNameItem
{
    public Guid Id { get; set; }

    [EntityColumn(256, true)]
    public string Name { get; set; } = string.Empty;

    [EntityColumn(512)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [EntityColumn(defaultSql: "GETUTCDATE()")]
    public DateTime CreatedAt { get; set; }
}
