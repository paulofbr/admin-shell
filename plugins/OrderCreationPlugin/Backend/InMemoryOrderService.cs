using AdminShell.Contracts;

namespace OrderCreationPlugin;

public sealed class InMemoryOrderService : IOrderService
{
    private readonly IOrderCatalogService _catalog;
    private readonly ISettingsAccessor<OrderCreationSettings> _settings;
    private readonly List<OrderDto> _orders = new();

    public InMemoryOrderService(
        IOrderCatalogService catalog,
        ISettingsAccessor<OrderCreationSettings> settings)
    {
        _catalog = catalog;
        _settings = settings;
    }

    public async Task<PagedOrdersResult> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var customers = await _catalog.GetCustomersAsync(ct);
        var customerById = new Dictionary<Guid, CustomerDto>();
        foreach (var customer in customers)
        {
            customerById[customer.Id] = customer;
        }

        var query = _orders
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var total = query.Count;
        var skip = Math.Max(page - 1, 0) * Math.Max(pageSize, 1);
        var data = query.Skip(skip).Take(Math.Max(pageSize, 1)).Select(o =>
        {
            customerById.TryGetValue(o.CustomerId, out var customer);
            return o with
            {
                CustomerName = customer?.Name ?? o.CustomerName
            };
        }).ToList();

        return new PagedOrdersResult(data, total, Math.Max(page, 1), Math.Max(pageSize, 1));
    }

    public async Task<OrderSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var todayStart = DateTimeOffset.UtcNow.Date;
        var openOrders = _orders.Count(o => o.Status == "Aberta");
        var openValue = _orders.Where(o => o.Status == "Aberta").Sum(o => o.Total);
        var todayRevenue = _orders
            .Where(o => o.CreatedAt >= todayStart && o.Status != "Cancelada")
            .Sum(o => o.Total);
        var products = await _catalog.GetProductsAsync(ct);
        var lowStockProducts = products.Count(p => p.Stock <= 10);

        return new OrderSummaryDto(
            openOrders,
            openValue,
            BuildOrderNumber(_orders.Count + 1),
            todayRevenue,
            lowStockProducts);
    }

    public async Task<CreateOrderResult> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        var customers = await _catalog.GetCustomersAsync(ct);
        var products = await _catalog.GetProductsAsync(ct);

        var customer = customers.FirstOrDefault(c => c.Id == request.CustomerId);
        if (customer is null)
        {
            return CreateOrderResult.Invalid(new Dictionary<string, string[]>
            {
                ["customerId"] = new[] { "Cliente não encontrado." }
            });
        }

        if (request.Lines.Count == 0)
        {
            return CreateOrderResult.Invalid(new Dictionary<string, string[]>
            {
                ["lines"] = new[] { "A encomenda deve ter pelo menos uma linha." }
            });
        }

        if (request.Lines.Count > settings.MaxItemsPerOrder)
        {
            return CreateOrderResult.Invalid(new Dictionary<string, string[]>
            {
                ["lines"] = new[] { $"A encomenda pode ter no máximo {settings.MaxItemsPerOrder} linhas." }
            });
        }

        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<OrderLineDto>();

        foreach (var requestLine in request.Lines)
        {
            var product = products.FirstOrDefault(p => p.Id == requestLine.ProductId);
            if (product is null)
            {
                AddError(errors, $"lines[{lines.Count}].productId", "Produto não encontrado.");
                continue;
            }

            if (requestLine.Quantity <= 0)
            {
                AddError(errors, $"lines[{lines.Count}].quantity", "A quantidade deve ser maior que zero.");
                continue;
            }

            if (settings.EnableInventoryCheck && requestLine.Quantity > product.Stock)
            {
                AddError(errors, $"lines[{lines.Count}].quantity", $"Stock insuficiente. Disponível: {product.Stock}.");
                continue;
            }

            var discountPercent = Math.Clamp(requestLine.DiscountPercent ?? 0, 0, 100);
            var unitPrice = product.UnitPrice * (1 - discountPercent / 100m);
            lines.Add(new OrderLineDto(
                product.Id,
                product.Sku,
                product.Name,
                requestLine.Quantity,
                product.UnitPrice,
                discountPercent,
                unitPrice * requestLine.Quantity));
        }

        if (errors.Count > 0)
        {
            return CreateOrderResult.Invalid(errors.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
        }

        var subtotal = lines.Sum(l => l.LineTotal);
        var vat = subtotal * 0.23m;
        var shippingCost = subtotal >= 250 ? 0 : 7.50m;
        var total = subtotal + vat + shippingCost;
        var orderNumber = BuildOrderNumber(_orders.Count + 1);

        var order = new OrderDto(
            Guid.NewGuid(),
            orderNumber,
            customer.Id,
            customer.Name,
            request.RequestedDeliveryDate is null
                ? DateTimeOffset.UtcNow.AddDays(5).Date
                : DateTimeOffset.Parse(request.RequestedDeliveryDate),
            "Aberta",
            subtotal,
            vat,
            shippingCost,
            total,
            "EUR",
            DateTimeOffset.UtcNow,
            lines);

        _orders.Insert(0, order);

        return CreateOrderResult.Successful(order);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = new List<string>();
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private static string BuildOrderNumber(int sequence)
        => $"ORD-2026-{sequence:000000}";
}
