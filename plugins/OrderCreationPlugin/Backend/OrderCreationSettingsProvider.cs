using AdminShell.Contracts;

namespace OrderCreationPlugin;

[Settings("order-creation")]
public sealed class OrderCreationSettings
{
    [Setting(
        "Enable inventory check",
        SettingType.Boolean,
        true,
        Description = "Check stock before creating an order.",
        Order = 10)]
    public bool EnableInventoryCheck { get; set; } = true;

    [Setting(
        "Max items per order",
        SettingType.Number,
        50,
        Description = "Maximum number of line items allowed per order.",
        Order = 20)]
    public int MaxItemsPerOrder { get; set; } = 50;

    [Setting(
        "Default observation",
        SettingType.Text,
        Description = "Default text added to new orders when no observation is provided.",
        Order = 30)]
    public string? DefaultObservation { get; set; }
}
