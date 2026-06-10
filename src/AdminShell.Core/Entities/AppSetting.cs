namespace AdminShell.Core.Entities;

public class AppSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string? Description { get; set; }
    public string ValueType { get; set; } = "string"; // string, boolean, number, text
}