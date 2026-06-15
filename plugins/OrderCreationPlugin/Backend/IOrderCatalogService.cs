namespace OrderCreationPlugin;

public interface IOrderCatalogService
{
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default);
}
