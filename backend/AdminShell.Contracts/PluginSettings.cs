namespace AdminShell.Contracts;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SettingsAttribute : Attribute
{
    public SettingsAttribute(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        Category = category;
    }

    public string Category { get; }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SettingAttribute : Attribute
{
    public SettingAttribute(
        string label,
        SettingType type = SettingType.String,
        object? defaultValue = null,
        bool required = false,
        int order = 100,
        string? description = null,
        string? validator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label;
        Type = type;
        DefaultValue = defaultValue;
        Required = required;
        Order = order;
        Description = description;
        Validator = validator;
    }

    public string Label { get; init; }
    public SettingType Type { get; init; }
    public object? DefaultValue { get; init; }
    public bool Required { get; init; }
    public int Order { get; init; }
    public string? Description { get; init; }
    public string? Validator { get; init; }
}

public enum SettingType
{
    String = 0,
    Boolean = 1,
    Number = 2,
    Text = 3,
    Json = 4
}

public sealed record SettingDefinition(
    string Category,
    string Name,
    string Label,
    SettingType Type = SettingType.String,
    string? DefaultValue = null,
    bool Required = false,
    int Order = 100,
    string? Description = null,
    string? Validator = null,
    Type? OptionsType = null,
    string? PropertyName = null)
{
    public string Key => $"settings.{Category}.{Name}";
    public string ValueType => Type switch
    {
        SettingType.Boolean => "boolean",
        SettingType.Number => "number",
        SettingType.Text => "text",
        SettingType.Json => "json",
        _ => "string"
    };
}

public sealed record SettingOptionDto(
    string Key,
    string Name,
    string Label,
    string? Description,
    SettingType Type,
    string? Value,
    string DefaultValue,
    bool Required,
    int Order,
    string? Validator);

public sealed record SettingsResponse(
    string Category,
    string Name,
    IReadOnlyList<SettingOptionDto> Settings);

public sealed record UpdateSettingRequest(string Key, string Value);

public interface ISettingsAccessor<TSettings> where TSettings : class, new()
{
    Task<TSettings> GetAsync(CancellationToken ct = default);
}
