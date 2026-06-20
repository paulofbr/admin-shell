using AdminShell.Contracts;

namespace OrderCreationPlugin;

[PermissionDefinitions("order-creation")]
public static class OrderCreationPermissions
{
    [PermissionDefinition("Orders", "Create")]
    public const string Create = "orders:create";

    [PermissionDefinition("Orders", "Read")]
    public const string Read = "orders:read";
}
