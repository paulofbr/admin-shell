namespace AdminShell.Contracts;

/// <summary>
/// Plugin that contributes API endpoints.
/// </summary>
public interface IApiPlugin : IAdminShellPlugin
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
