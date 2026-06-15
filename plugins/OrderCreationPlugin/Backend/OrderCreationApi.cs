using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrderCreationPlugin;

[PluginComponent(PluginId)]
public sealed class OrderCreationApi : IApiPlugin
{
    private const string PluginId = "order-creation";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("OrderCreationPlugin");

        var group = endpoints.MapPluginApi(PluginId);

        group.MapGet("/catalog/customers", async (IOrderCatalogService catalog, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/{PluginId}/catalog/customers", PluginId);
            return new OkObjectResult(await catalog.GetCustomersAsync(ct));
        })
        .WithName("GetOrderCustomers")
        .Produces<List<CustomerDto>>(StatusCodes.Status200OK);

        group.MapGet("/catalog/products", async (IOrderCatalogService catalog, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/{PluginId}/catalog/products", PluginId);
            return new OkObjectResult(await catalog.GetProductsAsync(ct));
        })
        .WithName("GetOrderProducts")
        .Produces<List<ProductDto>>(StatusCodes.Status200OK);

        group.MapGet("/orders", async (
            IOrderService orders,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            logger.LogInformation("GET /api/plugins/{PluginId}/orders page={Page} pageSize={PageSize}", PluginId, page, pageSize);
            return new OkObjectResult(await orders.GetPagedAsync(page, pageSize, ct));
        })
        .WithName("GetOrders")
        .Produces<PagedOrdersResult>(StatusCodes.Status200OK);

        group.MapGet("/orders/summary", async (IOrderService orders, CancellationToken ct) =>
        {
            logger.LogInformation("GET /api/plugins/{PluginId}/orders/summary", PluginId);
            return new OkObjectResult(await orders.GetSummaryAsync(ct));
        })
        .WithName("GetOrderSummary")
        .Produces<OrderSummaryDto>(StatusCodes.Status200OK);

        group.MapPost("/orders", async (
            CreateOrderRequest request,
            IOrderService orders,
            CancellationToken ct) =>
        {
            logger.LogInformation("POST /api/plugins/{PluginId}/orders", PluginId);
            var result = await orders.CreateAsync(request, ct);

            if (!result.Success)
            {
                var errors = result.Errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase);

                return new ObjectResult(errors) { StatusCode = StatusCodes.Status400BadRequest };
            }

            var order = result.Order ?? throw new InvalidOperationException("Order creation failed without an error payload.");
            return new CreatedResult($"/api/plugins/{PluginId}/orders/{order.Id}", order);
        })
        .WithName("CreateOrder")
        .Produces<CreateOrderResult>(StatusCodes.Status201Created)
        .Produces<Dictionary<string, string[]>>(StatusCodes.Status400BadRequest);
    }
}
