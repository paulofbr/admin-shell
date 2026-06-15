namespace OrderCreationPlugin;

public sealed class InMemoryOrderCatalogService : IOrderCatalogService
{
    private static readonly IReadOnlyList<CustomerDto> Customers = new[]
    {
        new CustomerDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Acme Lda", "501234567", "compras@acme.pt", "Lisboa"),
        new CustomerDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Beta Retail SA", "509876543", "ops@beta.pt", "Porto"),
        new CustomerDto(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Gamma Store", "504567890", "financeiro@gamma.pt", "Braga"),
        new CustomerDto(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Delta Market", "506543210", "procurement@delta.pt", "Coimbra")
    };

    private static readonly IReadOnlyList<ProductDto> Products = new[]
    {
        new ProductDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "SKU-100", "Teclado mecânico", 49.90m, "EUR", 28),
        new ProductDto(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "SKU-200", "Rato ergonómico", 34.50m, "EUR", 42),
        new ProductDto(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "SKU-300", "Monitor 27\"", 219.00m, "EUR", 11),
        new ProductDto(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "SKU-400", "Dock USB-C", 89.90m, "EUR", 19),
        new ProductDto(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "SKU-500", "Webcam HD", 64.00m, "EUR", 6)
    };

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken ct = default)
        => Task.FromResult(Customers);

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default)
        => Task.FromResult(Products);
}
