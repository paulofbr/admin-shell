using AdminShell.Contracts;

namespace OrderCreationPlugin;

[PluginComponent("order-creation")]
public sealed class OrderCreationHeaderActions : IHeaderActionPlugin
{
    public IEnumerable<HeaderActionDescriptor> GetHeaderActions()
    {
        yield return new HeaderActionDescriptor
        {
            Id = "new-order",
            Label = "Nova Encomenda",
            Icon = "Plus",
            Target = "header",
            ActionType = "route",
            ActionValue = "/orders/create",
            Order = 5,
            RequiredPermissions = new[] { "orders:create" }
        };
    }
}
