using AdminShell.Contracts;

namespace UserDepartmentPlugin;

public record DepartmentAssignmentRequest(string DepartmentId);

public class UserDepartmentPlugin : AdminShellPluginBase
{
    public override string Id => "user-department";

    public override string Name => "User Department Plugin";

    public override string Description => "Adds department management, filtering, and form fields to the users module";

    public override void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDepartmentService, DepartmentService>();
    }
}