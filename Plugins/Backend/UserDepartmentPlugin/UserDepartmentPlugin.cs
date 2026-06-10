using AdminShell.Contracts;

namespace UserDepartmentPlugin;

public class UserDepartmentPlugin : IAdminShellPlugin, IApiPlugin, IFormFieldPlugin, ISidebarSectionPlugin
{
    public string Id => "user-department";
    public string Name => "User Department Plugin";
    public string Version => "1.0.0";
    public string Description => "Adds department management, filtering, and form fields to the users module";

    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDepartmentService, DepartmentService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("UserDepartmentPlugin");

        var group = endpoints.MapGroup("/api/plugins/user-department")
            .WithTags("User Department (Plugin)");

        group.MapGet("/departments", async (IDepartmentService service, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/user-department/departments");
            var departments = await service.GetDepartmentsAsync(ct);
            return Results.Ok(departments);
        })
        .WithName("GetDepartments");

        group.MapGet("/users/{userId:guid}/department", async (
            Guid userId,
            IDepartmentService service,
            CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/user-department/users/{UserId}/department", userId);
            var dept = await service.GetUserDepartmentAsync(userId, ct);
            if (dept is null)
                return Results.NotFound();
            return Results.Ok(new { DepartmentId = dept });
        })
        .WithName("GetUserDepartment");
    }

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
            Options = new[]
            {
                new SelectOption("engineering", "Engineering", "Technical"),
                new SelectOption("design", "Design", "Creative"),
                new SelectOption("marketing", "Marketing", "Business"),
                new SelectOption("sales", "Sales", "Business"),
                new SelectOption("hr", "Human Resources", "Operations"),
                new SelectOption("finance", "Finance", "Operations"),
                new SelectOption("support", "Customer Support", "Operations"),
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
            Options = new[]
            {
                new SelectOption("engineering", "Engineering", "Technical"),
                new SelectOption("design", "Design", "Creative"),
                new SelectOption("marketing", "Marketing", "Business"),
                new SelectOption("sales", "Sales", "Business"),
                new SelectOption("hr", "Human Resources", "Operations"),
                new SelectOption("finance", "Finance", "Operations"),
                new SelectOption("support", "Customer Support", "Operations"),
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

    public IEnumerable<SidebarSectionDescriptor> GetSidebarSections()
    {
        yield return new SidebarSectionDescriptor
        {
            Id = "department-tools",
            Label = "Departments",
            Icon = "OfficeBuilding",
            Order = 50,
            Items = new[]
            {
                new SidebarMenuItem
                {
                    Id = "dept-overview",
                    Label = "Department Overview",
                    Icon = "DataBoard",
                    Path = "/departments",
                    Order = 10
                },
                new SidebarMenuItem
                {
                    Id = "dept-members",
                    Label = "Members by Department",
                    Icon = "UserFilled",
                    Path = "/departments/members",
                    Order = 20
                },
                new SidebarMenuItem
                {
                    Id = "dept-report",
                    Label = "Department Report",
                    Icon = "DataAnalysis",
                    Path = "/departments/report",
                    Order = 30
                }
            }
        };
    }
}