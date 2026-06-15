using AdminShell.Contracts;

namespace OrderCreationPlugin;

[PluginComponent("order-creation")]
public sealed class OrderCreationMenu : IMenuPlugin
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        yield return new MenuItem
        {
            Id = "orders",
            Label = "Encomendas",
            Path = "/orders",
            Icon = "Document",
            Order = 20
        };

        yield return new MenuItem
        {
            Id = "orders-summary",
            Label = "Resumo de Encomendas",
            Path = "/orders/summary",
            Icon = "DataAnalysis",
            ParentId = "orders",
            Order = 21
        };

        yield return new MenuItem
        {
            Id = "orders-create",
            Label = "Criar Encomenda",
            Path = "/orders/create",
            Icon = "Plus",
            ParentId = "orders",
            Order = 22
        };
    }
}
