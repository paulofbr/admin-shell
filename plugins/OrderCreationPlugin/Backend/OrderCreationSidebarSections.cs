using AdminShell.Contracts;

namespace OrderCreationPlugin;

[PluginComponent("order-creation")]
public sealed class OrderCreationSidebarSections : ISidebarSectionPlugin
{
    public IEnumerable<SidebarSectionDescriptor> GetSidebarSections()
    {
        yield return new SidebarSectionDescriptor
        {
            Id = "order-creation-section",
            Label = "Encomendas",
            Icon = "Document",
            Order = 20,
            Items = new[]
            {
                new SidebarMenuItem
                {
                    Id = "orders-overview",
                    Label = "Visão geral",
                    Icon = "DataAnalysis",
                    Path = "/orders",
                    Order = 10
                },
                new SidebarMenuItem
                {
                    Id = "orders-summary-menu",
                    Label = "Resumo",
                    Icon = "DataAnalysis",
                    Path = "/orders/summary",
                    Order = 20
                },
                new SidebarMenuItem
                {
                    Id = "orders-create-menu",
                    Label = "Criar",
                    Icon = "Plus",
                    Path = "/orders/create",
                    Order = 30
                }
            }
        };
    }
}
