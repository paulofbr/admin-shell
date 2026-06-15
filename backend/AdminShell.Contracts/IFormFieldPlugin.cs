namespace AdminShell.Contracts;

/// <summary>
/// Describes an extra form field contributed by a plugin.
/// </summary>
public record FormFieldDescriptor
{
    /// <summary>Unique field key (e.g., "department", "employeeId").</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Display label for the field.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Form type the field belongs to.
    /// Built-in targets: 'user.create', 'user.edit', 'user.profile', 'role.edit'
    /// </summary>
    public string TargetForm { get; init; } = "user.edit";

    /// <summary>HTML input type: 'text', 'number', 'select', 'checkbox', 'date', 'textarea'.</summary>
    public string InputType { get; init; } = "text";

    /// <summary>Whether the field is required.</summary>
    public bool Required { get; init; }

    /// <summary>Default value.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>Placeholder text.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Validation regex pattern (optional).</summary>
    public string? ValidationPattern { get; init; }

    /// <summary>Validation error message (optional).</summary>
    public string? ValidationMessage { get; init; }

    /// <summary>Description / help text shown below the field.</summary>
    public string? Description { get; init; }

    /// <summary>For 'select' input type: available options.</summary>
    public SelectOption[]? Options { get; init; }

    /// <summary>Sort order among fields.</summary>
    public int Order { get; init; } = 100;

    /// <summary>Optional endpoint used by the shell to persist the field value.</summary>
    public string? ApiEndpoint { get; init; }

    /// <summary>Optional endpoint used by the shell to load the field value. Supports {userId} placeholders.</summary>
    public string? LoadValueEndpoint { get; init; }

    /// <summary>Optional JSON path used when loading a scalar value from an endpoint response.</summary>
    public string? ValuePath { get; init; }

    /// <summary>Optional HTTP method for persisting the field value.</summary>
    public string? SubmitMethod { get; init; }

    /// <summary>Optional request payload property name for persisting the field value.</summary>
    public string? PayloadPath { get; init; }
}

/// <summary>Key-value pair for select dropdown options.</summary>
public record SelectOption(string Value, string Label, string? Group = null);

/// <summary>
/// Plugin that contributes extra fields to shell forms.
/// </summary>
public interface IFormFieldPlugin : IPluginComponent
{
    /// <summary>
    /// Returns extra fields to be added to forms.
    /// </summary>
    IEnumerable<FormFieldDescriptor> GetFormFields();
}