using AdminShell.Contracts;

namespace UserDepartmentPlugin;

[PluginComponent("user-department")]
public sealed class UserExtensionFields : IExtensionFieldPlugin
{
    public IEnumerable<EntityExtensionFieldDefinition> GetExtensionFields()
    {
        yield return new EntityExtensionFieldDefinition(
            EntityName: "User",
            Name: "Department",
            Type: EntityExtensionFieldType.Select,
            Required: false,
            Label: "Department",
            PossibleValues: FieldPossibleValues.Values("Engineering", "Marketing", "Sales", "Human Resources", "Finance", "Operations"),
            FrontEndValidator: "entity => ({ ok: true })",
            Order: 50,
            Description: "User's primary department",
            Slot: "user.department");

        yield return new EntityExtensionFieldDefinition(
            EntityName: "User",
            Name: "EmployeeId",
            Type: EntityExtensionFieldType.String,
            Required: false,
            Label: "Employee ID",
            FrontEndValidator: "entity => { const value = entity.extensionFields?.find(f => f.name === 'EmployeeId')?.value; return !value || /^EMP-\\d{3,}$/.test(String(value)) ? { ok: true } : { ok: false, message: 'Must follow format EMP-XXX (e.g., EMP-001)' }; }",
            Order: 60,
            Description: "Internal employee identifier",
            Slot: "user.employeeId");
    }
}
