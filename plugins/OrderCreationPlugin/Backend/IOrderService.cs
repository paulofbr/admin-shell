namespace OrderCreationPlugin;

public interface IOrderService
{
    Task<PagedOrdersResult> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<OrderSummaryDto> GetSummaryAsync(CancellationToken ct = default);

    Task<CreateOrderResult> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
}
