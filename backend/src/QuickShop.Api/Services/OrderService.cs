using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services.Payment;
using DomainPayment = QuickShop.Api.Domain.Payment;

namespace QuickShop.Api.Services;

/// <summary>
/// Order placement and history. Checkout runs in a single database transaction:
/// order rows + address snapshot + conditional stock decrements either all commit
/// or none do. The payment provider is only consulted afterwards, and the order is
/// marked paid ONLY when the provider confirms success.
/// </summary>
public class OrderService
{
    private readonly QuickShopDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentProcessor _paymentProcessor;

    public OrderService(QuickShopDbContext db, ICurrentUser currentUser, IPaymentProcessor paymentProcessor)
    {
        _db = db;
        _currentUser = currentUser;
        _paymentProcessor = paymentProcessor;
    }

    private string UserId => _currentUser.UserId
        ?? throw new UnauthorizedException("Sign in to continue.");

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        var userId = UserId;

        // --- shipping address (saved address id, or a new one that gets saved) ---
        Address address;
        if (request.AddressId is int addressId)
        {
            address = await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct)
                ?? throw new NotFoundException("Address");
        }
        else if (request.NewAddress is not null)
        {
            address = MapNewAddress(userId, request.NewAddress);
            var isFirst = !await _db.Addresses.AnyAsync(a => a.UserId == userId, ct);
            if (address.IsDefault || isFirst)
            {
                await ResetDefaultAddressesAsync(userId, ct);
                address.IsDefault = true;
            }
            _db.Addresses.Add(address);
        }
        else
        {
            throw new ValidationException("Please correct the errors below.",
                new Dictionary<string, string[]> { ["address"] = ["Choose a shipping address or enter a new one."] });
        }

        // --- cart ---
        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .Include(c => c.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null || cart.Items.Count == 0)
            throw new ValidationException("Please correct the errors below.",
                new Dictionary<string, string[]> { ["cart"] = ["Your cart is empty."] });

        // Fast pre-checks before opening the transaction.
        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
                throw new ConflictException($"{item.Product.Name} is no longer available.");

            var available = item.Variant?.Stock ?? item.Product.Stock;
            if (item.Quantity > available)
                throw new ConflictException($"Only {available} unit(s) of {item.Product.Name} are in stock.");
        }

        var method = string.Equals(request.ShippingMethod, "express", StringComparison.OrdinalIgnoreCase)
            ? ShippingMethod.Express
            : ShippingMethod.Standard;

        var subtotal = cart.Items.Sum(i => Pricing.UnitPrice(i.Product, i.Variant) * i.Quantity);
        var shippingCost = Pricing.ShippingCost(method, subtotal);
        var total = subtotal + shippingCost;

        // EnableRetryOnFailure execution strategies own their transactions: the whole
        // commit unit (order rows + stock decrements) runs inside CreateExecutionStrategy
        // so a transient failure can safely retry the entire unit.
        var strategy = _db.Database.CreateExecutionStrategy();

        var (order, orderItems) = await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var newOrder = new Order
            {
                OrderNumber = await NextOrderNumberAsync(ct),
                UserId = userId,
                Status = OrderStatus.Pending,
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Total = total,
                ShippingMethod = method,
                PaymentMethod = request.PaymentMethod,
                ShipFullName = address.FullName,
                ShipPhone = address.Phone,
                ShipLine1 = address.Line1,
                ShipLine2 = address.Line2,
                ShipCity = address.City,
                ShipState = address.State,
                ShipPostalCode = address.PostalCode,
                ShipCountry = address.Country,
                CustomerNote = request.CustomerNote?.Trim(),
                PlacedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var item in cart.Items)
            {
                var unitPrice = Pricing.UnitPrice(item.Product, item.Variant);
                newOrder.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ProductName = item.Product.Name,
                    ProductSlug = item.Product.Slug,
                    Sku = item.Product.Sku,
                    ProductImageUrl = item.Product.ImageUrl,
                    VariantName = item.Variant?.Name,
                    UnitPrice = unitPrice,
                    Quantity = item.Quantity,
                    LineTotal = unitPrice * item.Quantity
                });
            }

            newOrder.Payment = new DomainPayment
            {
                Provider = _paymentProcessor.ProviderName,
                Amount = total,
                Currency = "INR",
                Status = PaymentStatus.Pending,
                CardLast4 = request.CardLast4,
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(newOrder);
            await _db.SaveChangesAsync(ct);

            // --- conditional stock decrements (all-or-nothing) ---
            foreach (var item in cart.Items)
            {
                var productAffected = await _db.Products
                    .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity), ct);

                if (productAffected == 0)
                {
                    // Disposing the un-committed transaction rolls everything back.
                    throw new ConflictException($"Not enough stock left for {item.Product.Name}. Your order was not placed.");
                }

                if (item.VariantId is int variantId)
                {
                    var variantAffected = await _db.ProductVariants
                        .Where(v => v.Id == variantId && v.Stock >= item.Quantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock - item.Quantity), ct);

                    if (variantAffected == 0)
                    {
                        throw new ConflictException($"Not enough stock left for {item.Product.Name} ({item.Variant?.Name}). Your order was not placed.");
                    }
                }
            }

            await tx.CommitAsync(ct);

            // Snapshot for cleanup/restock after payment resolution.
            var snapshot = newOrder.Items
                .Select(i => new { i.ProductId, i.VariantId, i.Quantity, ProductName = i.ProductName })
                .ToList();

            return (newOrder, snapshot);
        });

        // --- charge via the payment provider (outside the DB transaction) ---
        var result = await _paymentProcessor.ChargeAsync(
            new PaymentRequest(order.Id, order.OrderNumber, total, "INR", request.PaymentMethod, request.CardLast4),
            ct);

        var payment = order.Payment!;
        payment.ProcessedAt = DateTime.UtcNow;

        if (!result.Success)
        {
            // Provider declined → order is NOT confirmed; stock goes back; cart is preserved.
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = result.FailureReason ?? "The payment provider declined the transaction.";
            payment.ProviderTransactionId = result.TransactionId;
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            foreach (var line in orderItems)
            {
                if (line.ProductId is int productId)
                {
                    await _db.Products
                        .Where(p => p.Id == productId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + line.Quantity), ct);
                }

                if (line.VariantId is int variantId)
                {
                    await _db.ProductVariants
                        .Where(v => v.Id == variantId)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock + line.Quantity), ct);
                }
            }

            await _db.SaveChangesAsync(ct);
            throw new PaymentFailedException(order.OrderNumber, payment.FailureReason);
        }

        // Provider confirmed → only now is the order paid.
        payment.Status = PaymentStatus.Succeeded;
        payment.ProviderTransactionId = result.TransactionId;
        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        var purchasedLines = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
        _db.CartItems.RemoveRange(purchasedLines);

        await _db.SaveChangesAsync(ct);

        var detail = await LoadDetailAsync(order.Id, ct);
        return new CheckoutResponse(detail, detail.Payment!);
    }

    // ---- customer history ------------------------------------------------

    public async Task<PagedResult<OrderSummaryDto>> ListMineAsync(int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var userId = UserId;

        var query = _db.Orders.AsNoTracking().Where(o => o.UserId == userId);
        var total = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .AsNoTracking()
            .ToListAsync(ct);

        return PagedResult<OrderSummaryDto>.Create(orders.Select(o => ToSummary(o)).ToList(), page, pageSize, total);
    }

    public async Task<OrderDetailDto> GetMineAsync(int id, CancellationToken ct = default)
    {
        var userId = UserId;
        // Ownership check: other users' orders must look like 404s.
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, ct)
            ?? throw new NotFoundException("Order");

        return ToDetail(order);
    }

    // ---- admin -----------------------------------------------------------

    public async Task<PagedResult<OrderSummaryDto>> ListAdminAsync(
        string? status, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);

        var query = _db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
            query = query.Where(o => o.Status == parsed);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                o.User!.Email!.Contains(term) ||
                o.User.FullName.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .Include(o => o.User)
            .AsNoTracking()
            .ToListAsync(ct);

        return PagedResult<OrderSummaryDto>.Create(orders.Select(o => ToSummary(o, includeCustomer: true)).ToList(), page, pageSize, total);
    }

    public async Task<OrderDetailDto> GetAdminAsync(int id, CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Order");

        return ToDetail(order);
    }

    public async Task<OrderDetailDto> UpdateStatusAsync(int id, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var newStatus))
            throw new ValidationException("Please correct the errors below.",
                new Dictionary<string, string[]> { ["status"] = ["Unknown order status."] });

        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Order");

        if (!OrderStatusTransitions.CanTransition(order.Status, newStatus))
            throw new ConflictException($"An order cannot move from {order.Status} to {newStatus}.");

        var previous = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (newStatus == OrderStatus.Cancelled && previous != OrderStatus.Cancelled)
        {
            // Give the stock back.
            foreach (var item in order.Items)
            {
                if (item.ProductId is int productId)
                {
                    await _db.Products
                        .Where(p => p.Id == productId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
                }

                if (item.VariantId is int variantId)
                {
                    await _db.ProductVariants
                        .Where(v => v.Id == variantId)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock + item.Quantity), ct);
                }
            }

            if (order.Payment is { Status: PaymentStatus.Succeeded })
            {
                order.Payment.Status = PaymentStatus.Refunded;
                order.Payment.ProcessedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return await GetAdminAsync(order.Id, ct);
    }

    // ---- helpers ---------------------------------------------------------

    /// <summary>Loads an order the caller already owns (used after checkout mutates it).</summary>
    private async Task<OrderDetailDto> LoadDetailAsync(int id, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Order");

        return ToDetail(order);
    }

    private async Task<string> NextOrderNumberAsync(CancellationToken ct)
    {
        var next = (await _db.Orders.MaxAsync(o => (int?)o.Id, ct) ?? 0) + 1;
        return $"ORD{next:D7}";
    }

    private static Address MapNewAddress(string userId, AddressUpsertRequest request) => new()
    {
        UserId = userId,
        FullName = request.FullName.Trim(),
        Phone = request.Phone.Trim(),
        Line1 = request.Line1.Trim(),
        Line2 = request.Line2?.Trim(),
        City = request.City.Trim(),
        State = request.State.Trim(),
        PostalCode = request.PostalCode.Trim(),
        Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim(),
        IsDefault = request.IsDefault,
        CreatedAt = DateTime.UtcNow
    };

    private async Task ResetDefaultAddressesAsync(string userId, CancellationToken ct)
    {
        var defaults = await _db.Addresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(ct);

        foreach (var address in defaults)
            address.IsDefault = false;
    }

    public static OrderSummaryDto ToSummary(Order order, bool includeCustomer = false) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        Status = order.Status.ToString(),
        Subtotal = order.Subtotal,
        ShippingCost = order.ShippingCost,
        Total = order.Total,
        PlacedAt = order.PlacedAt,
        ItemCount = order.Items.Sum(i => i.Quantity),
        ThumbnailUrl = order.Items.FirstOrDefault()?.ProductImageUrl,
        CustomerName = includeCustomer ? order.User?.FullName : null,
        CustomerEmail = includeCustomer ? order.User?.Email : null
    };

    private static OrderDetailDto ToDetail(Order order)
    {
        var summary = ToSummary(order, includeCustomer: true);
        return new OrderDetailDto
        {
            Id = summary.Id,
            OrderNumber = summary.OrderNumber,
            Status = summary.Status,
            Subtotal = summary.Subtotal,
            ShippingCost = summary.ShippingCost,
            Total = summary.Total,
            PlacedAt = summary.PlacedAt,
            ItemCount = summary.ItemCount,
            ThumbnailUrl = summary.ThumbnailUrl,
            CustomerName = summary.CustomerName,
            CustomerEmail = summary.CustomerEmail,
            ShippingMethod = order.ShippingMethod.ToString().ToLowerInvariant(),
            PaymentMethod = order.PaymentMethod,
            CustomerNote = order.CustomerNote,
            ShipFullName = order.ShipFullName,
            ShipPhone = order.ShipPhone,
            ShipLine1 = order.ShipLine1,
            ShipLine2 = order.ShipLine2,
            ShipCity = order.ShipCity,
            ShipState = order.ShipState,
            ShipPostalCode = order.ShipPostalCode,
            ShipCountry = order.ShipCountry,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Slug = i.ProductSlug,
                Sku = i.Sku,
                ImageUrl = i.ProductImageUrl,
                VariantName = i.VariantName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList(),
            Payment = order.Payment is null ? null : new PaymentDto
            {
                Provider = order.Payment.Provider,
                Status = order.Payment.Status.ToString(),
                TransactionId = order.Payment.ProviderTransactionId,
                CardLast4 = order.Payment.CardLast4,
                FailureReason = order.Payment.FailureReason,
                Amount = order.Payment.Amount,
                ProcessedAt = order.Payment.ProcessedAt
            }
        };
    }
}
