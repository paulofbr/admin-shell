using AdminShell.Contracts;

namespace UserDepartmentPlugin;

[PluginComponent("user-department")]
public sealed class DepartmentFormFields : IFormFieldPlugin
{
    public IEnumerable<FormFieldDescriptor> GetFormFields()
    {
        yield return new FormFieldDescriptor
        {
            Key = "department",
            Label = "Department",
            TargetForm = "user.create",
            InputType = "select",
            Required = false,
            Placeholder = "Select a department",
            Description = "User's primary department",
            Order = 50,
            ApiEndpoint = "/api/plugins/user-department/users/{userId}/department",
            LoadValueEndpoint = "/api/plugins/user-department/users/{userId}/department",
            ValuePath = "departmentId",
            PayloadPath = "DepartmentId",
            Options = new[]
            {
                new SelectOption("eng", "Engineering", "Technical"),
                new SelectOption("marketing", "Marketing", "Business"),
                new SelectOption("sales", "Sales", "Business"),
                new SelectOption("hr", "Human Resources", "Operations"),
                new SelectOption("finance", "Finance", "Operations"),
                new SelectOption("ops", "Operations", "Operations"),
            }
        };

        yield return new FormFieldDescriptor
        {
            Key = "department",
            Label = "Department",
            TargetForm = "user.edit",
            InputType = "select",
            Required = false,
            Placeholder = "Select a department",
            Description = "User's primary department",
            Order = 50,
            ApiEndpoint = "/api/plugins/user-department/users/{userId}/department",
            LoadValueEndpoint = "/api/plugins/user-department/users/{userId}/department",
            ValuePath = "departmentId",
            PayloadPath = "DepartmentId",
            Options = new[]
            {
                new SelectOption("eng", "Engineering", "Technical"),
                new SelectOption("marketing", "Marketing", "Business"),
                new SelectOption("sales", "Sales", "Business"),
                new SelectOption("hr", "Human Resources", "Operations"),
                new SelectOption("finance", "Finance", "Operations"),
                new SelectOption("ops", "Operations", "Operations"),
            }
        };

        yield return new FormFieldDescriptor
        {
            Key = "employeeId",
            Label = "Employee ID",
            TargetForm = "user.create",
            InputType = "text",
            Required = false,
            Placeholder = "e.g., EMP-001",
            Description = "Internal employee identifier",
            ValidationPattern = @"^EMP-\d{3,}$",
            ValidationMessage = "Must follow format EMP-XXX (e.g., EMP-001)",
            Order = 60
        };
    }
}
