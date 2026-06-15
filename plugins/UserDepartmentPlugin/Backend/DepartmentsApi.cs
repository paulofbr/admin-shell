using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace UserDepartmentPlugin;

[PluginComponent(PluginId)]
public sealed class DepartmentsApi : IApiPlugin
{
    private const string PluginId = "user-department";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("UserDepartmentPlugin");

        var group = endpoints.MapPluginApi(PluginId);

        group.MapGet("/departments", async (IDepartmentService service, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/user-department/departments");
            var departments = await service.GetDepartmentsAsync(ct);
            return Results.Ok(departments);
        })
        .WithName("GetDepartments")
        .Produces<List<DepartmentDto>>(StatusCodes.Status200OK);

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
        .WithName("GetUserDepartment")
        .Produces<GetUserDepartmentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/users/{userId:guid}/department", async (
            Guid userId,
            DepartmentAssignmentRequest request,
            IDepartmentService service,
            CancellationToken ct) =>
        {
            logger.LogInformation("PUT /api/plugins/user-department/users/{UserId}/department", userId);
            await service.SetUserDepartmentAsync(userId, request.DepartmentId, ct);
            return Results.Ok(new { DepartmentId = request.DepartmentId });
        })
        .WithName("SetUserDepartment")
        .Produces<SetUserDepartmentResponse>(StatusCodes.Status200OK);
    }
}
