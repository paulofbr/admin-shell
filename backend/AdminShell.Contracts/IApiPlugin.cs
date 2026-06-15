namespace AdminShell.Contracts;

/// <summary>
/// Plugin that contributes API endpoints.
/// </summary>
public interface IApiPlugin : IPluginComponent
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
