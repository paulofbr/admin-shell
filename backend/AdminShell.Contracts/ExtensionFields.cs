using System.Text.Json.Serialization;

namespace AdminShell.Contracts;

public sealed class FieldPossibleValues
{
    [System.Text.Json.Serialization.JsonPropertyName("values")]
    public string[]? ValueList { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("enumName")]
    public string? EnumName { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("uri")]
    public string? UriValue { get; init; }

    public static FieldPossibleValues Values(params string[] values)
        => new() { ValueList = values };

    public static FieldPossibleValues Values(IEnumerable<string> values)
        => new() { ValueList = values.ToArray() };

    public static FieldPossibleValues Enum<T>() where T : struct, Enum
        => new() { EnumName = typeof(T).Name };

    public static FieldPossibleValues Uri(string uri)
        => new() { UriValue = uri };
}

public enum EntityExtensionFieldType
{
    String = 0,
    Text = 1,
    Number = 2,
    Boolean = 3,
    Date = 4,
    DateTime = 5,
    Select = 6,
    Json = 7,
    Array = 8
}

public sealed record EntityExtensionFieldDefinition(
    string EntityName,
    string Name,
    EntityExtensionFieldType Type = EntityExtensionFieldType.String,
    bool Required = false,
    object? DefaultValue = null,
    string Label = "",
    FieldPossibleValues? PossibleValues = null,
    string FrontEndValidator = "",
    int Order = 100,
    string? Description = null,
    string? Slot = null)
{
    public string ColumnName => $"CDU_{ToColumnPart(Name)}";

    public string TableName => EntityExtensionFieldDefinition.GetTableName(EntityName);

    public string QuotedTableName => $"[{TableName}]";

    public string QuotedColumnName => $"[{ColumnName}]";

    public string SqlType => EntityExtensionFieldDefinition.GetSqlType(Type);

    private static string ToColumnPart(string value)
    {
        var chars = value
            .Select(c => char.IsLetterOrDigit(c) ? c : '_')
            .ToArray();

        var normalized = new string(chars)
            .Trim('_')
            .Replace("__", "_", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"Extension field name '{value}' cannot be converted to a SQL column name.");

        if (char.IsDigit(normalized[0]))
            normalized = $"F_{normalized}";

        return normalized;
    }

    public static string GetTableName(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new InvalidOperationException("Entity name is required.");

        if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return entityName;

        if (entityName.EndsWith("y", StringComparison.OrdinalIgnoreCase)
            && entityName.Length > 1
            && !IsVowel(entityName[^2]))
        {
            return $"{entityName[..^1]}ies";
        }

        return $"{entityName}s";
    }

    private static bool IsVowel(char value) => "aeiou".Contains(char.ToLowerInvariant(value), StringComparison.Ordinal);

    private static string GetSqlType(EntityExtensionFieldType type) => type switch
    {
        EntityExtensionFieldType.Boolean => "BIT",
        EntityExtensionFieldType.Number => "DECIMAL(18,4)",
        EntityExtensionFieldType.Date => "DATETIME2",
        EntityExtensionFieldType.DateTime => "DATETIME2",
        EntityExtensionFieldType.Json or EntityExtensionFieldType.Array => "NVARCHAR(MAX)",
        _ => "NVARCHAR(MAX)"
    };
}

public interface IExtensionFieldPlugin : IPluginComponent
{
    IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields();
}
