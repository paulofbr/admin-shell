using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PluginName.Entities;
using PluginName.Services;

namespace PluginName.Apis;

public sealed class PluginNameItemApi : IApiPlugin
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapPluginApi("plugin-id");

        group.MapGet("/items", async (IPluginNameItemService service, CancellationToken ct) =>
        {
            return Results.Ok(await service.ListAsync(ct));
        })
        .WithName("GetPluginNameItems")
        .Produces<List<PluginNameItem>>(StatusCodes.Status200OK);
    }
}
