using AdminShell.Contracts;

namespace OrderCreationPlugin;

[PluginComponent("order-creation")]
public sealed class OrderCreationWidgets : IWidgetPlugin
{
    public IEnumerable<WidgetDescriptor> GetWidgets()
    {
        yield return new WidgetDescriptor
        {
            Id = "order-quick-create",
            Title = "Criação rápida de encomenda",
            Zone = "dashboard",
            Width = 6,
            Height = 5,
            Order = 5,
            ComponentName = "OrderQuickCreateWidget",
            Settings = new Dictionary<string, object>
            {
                ["description"] = "Widget Vue que consulta catálogo e cria encomenda via API do plugin.",
                ["requiresPermissions"] = new[] { OrderCreationPermissions.Create }
            }
        };
    }
}
