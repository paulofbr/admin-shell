using System.Text.Json.Serialization;
using AdminShell.Contracts;
using AdminShell.Core.Interfaces;

namespace AdminShell.Core.Entities;

public sealed class ExtensionField
{
    public string Name { get; set; } = string.Empty;

    public object? Value { get; set; }

    public string Type { get; set; } = "string";

    public bool Required { get; set; }

    public object? DefaultValue { get; set; }

    public string Label { get; set; } = string.Empty;

    public FieldPossibleValues? PossibleValues { get; set; }

    public string FrontEndValidator { get; set; } = string.Empty;

    public string? Slot { get; set; }

    [JsonIgnore]
    public Func<BaseEntity, Result>? BackendValidator { get; set; }

    [JsonIgnore]
    public Func<BaseEntity, Result>? Validator
    {
        get => BackendValidator;
        set => BackendValidator = value;
    }
}
