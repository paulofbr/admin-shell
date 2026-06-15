using AdminShell.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrderCreationPlugin;

public class OrderCreationPlugin : AdminShellPluginBase
{
    public override string Id => "order-creation";

    public override string Name => "Order Creation Plugin";

    public override string Description => "Exemplo complexo de plugin para criação de encomendas com frontend Vue SFC em TypeScript empacotado em pasta separada.";

    public override void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOrderCatalogService, InMemoryOrderCatalogService>();
        services.AddSingleton<IOrderService, InMemoryOrderService>();

        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("OrderCreationPlugin");
        logger.LogInformation("OrderCreationPlugin initialized — catalog + order API + separate Vue SFC frontend package ready");
    }
}
