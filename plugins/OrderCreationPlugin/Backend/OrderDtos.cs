namespace OrderCreationPlugin;

public record CustomerDto(Guid Id, string Name, string Nif, string Email, string City);

public record ProductDto(Guid Id, string Sku, string Name, decimal UnitPrice, string Currency, int Stock);

public record OrderLineDto(
    Guid ProductId,
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateTimeOffset RequestedDeliveryDate,
    string Status,
    decimal Subtotal,
    decimal Vat,
    decimal ShippingCost,
    decimal Total,
    string Currency,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDto> Lines);

public record CreateOrderRequest(
    Guid CustomerId,
    string? PoNumber,
    string? Notes,
    string? RequestedDeliveryDate,
    ShippingAddressRequest ShippingAddress,
    string PaymentMethod,
    IReadOnlyList<CreateOrderLineRequest> Lines);

public record CreateOrderLineRequest(Guid ProductId, int Quantity, decimal? DiscountPercent);

public record ShippingAddressRequest(string Street, string City, string PostalCode, string Country);

public record PagedOrdersResult(IReadOnlyList<OrderDto> Data, int Total, int Page, int PageSize);

public record OrderSummaryDto(
    int OpenOrders,
    decimal OpenValue,
    string NextOrderNumber,
    decimal TodayRevenue,
    int LowStockProducts);

public record CreateOrderResult(bool Success, OrderDto? Order, Dictionary<string, string[]> Errors)
{
    public static CreateOrderResult Successful(OrderDto order)
        => new(true, order, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

    public static CreateOrderResult Invalid(Dictionary<string, string[]> errors)
        => new(false, null, errors);
}
