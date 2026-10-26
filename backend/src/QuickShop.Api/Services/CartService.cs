using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;

namespace QuickShop.Api.Services;

/// <summary>
/// Server-side cart. Works for guests (opaque client-generated id) and authenticated
/// users (persisted per user id). Every request re-validates ownership and re-derives
/// prices from the database — nothing is trusted from the client.
/// </summary>
public class CartService
{
    public const string GuestHeader = "X-Cart-Id";

    private readonly QuickShopDbContext _db;
    private readonly Security.ICurrentUser _currentUser;

    public CartService(QuickShopDbContext db, Security.ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CartDto> GetCartAsync(string? guestId)
    {
        var cart = await ResolveAsync(guestId, forWrite: false);
        return cart is null ? EmptyCart(_currentUser.IsAuthenticated ? "user" : "guest") : Map(cart);
    }

    public async Task<CartDto> AddItemAsync(string? guestId, AddToCartRequest request, CancellationToken ct = default)
    {
        var cart = await ResolveAsync(guestId, forWrite: true, ct)
            ?? throw new ConflictException("Could not resolve a cart for this request.");

        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product");

        if (!product.IsActive)
            throw new ConflictException("This product is no longer available.");

        ProductVariant? variant = null;
        if (request.VariantId is int variantId)
        {
            variant = product.Variants.FirstOrDefault(v => v.Id == variantId && v.IsActive)
                ?? throw new NotFoundException("Variant");
        }

        var available = variant?.Stock ?? product.Stock;
        if (available <= 0)
            throw new ConflictException($"{product.Name} is out of stock.");

        var requested = Math.Min(request.Quantity, Pricing.MaxCartQuantity);
        if (requested > available)
            throw new ConflictException($"Only {available} unit(s) of {product.Name} are in stock.");

        // Hoisted out: EF expression trees cannot contain the null-propagating operator.
        int? requestedVariantId = variant?.Id;
        var item = await _db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id
                                      && i.ProductId == product.Id
                                      && i.VariantId == requestedVariantId, ct);

        if (item is null)
        {
            item = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = variant?.Id,
                Quantity = requested
            };
            _db.CartItems.Add(item);
        }
        else
        {
            var newQty = item.Quantity + requested;
            if (newQty > available)
                throw new ConflictException($"Only {available} unit(s) of {product.Name} are in stock.");
            item.Quantity = newQty;
            item.UpdatedAt = DateTime.UtcNow;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetCartAsync(guestId);
    }

    public async Task<CartDto> UpdateItemAsync(string? guestId, int itemId, int quantity, CancellationToken ct = default)
    {
        var cart = await ResolveAsync(guestId, forWrite: true, ct)
            ?? throw new ConflictException("Could not resolve a cart for this request.");

        var item = await _db.CartItems
            .Include(i => i.Product)
            .Include(i => i.Variant)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, ct)
            ?? throw new NotFoundException("Cart item");

        var available = item.Variant?.Stock ?? item.Product.Stock;
        if (quantity > available)
            throw new ConflictException($"Only {available} unit(s) of {item.Product.Name} are in stock.");

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetCartAsync(guestId);
    }

    public async Task<CartDto> RemoveItemAsync(string? guestId, int itemId, CancellationToken ct = default)
    {
        var cart = await ResolveAsync(guestId, forWrite: true, ct)
            ?? throw new ConflictException("Could not resolve a cart for this request.");

        var item = await _db.CartItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, ct)
            ?? throw new NotFoundException("Cart item");

        _db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetCartAsync(guestId);
    }

    public async Task<CartDto> ClearAsync(string? guestId, CancellationToken ct = default)
    {
        var cart = await ResolveAsync(guestId, forWrite: true, ct);
        if (cart is not null && cart.Items.Count > 0)
        {
            var items = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
            _db.CartItems.RemoveRange(items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return cart is null ? EmptyCart(_currentUser.IsAuthenticated ? "user" : "guest") : Map(cart);
    }

    /// <summary>
    /// Merges the caller's guest cart into their user cart after sign-in.
    /// The guest cart must match the X-Cart-Id header and must be guest-owned.
    /// </summary>
    public async Task<CartDto> MergeGuestCartAsync(string? guestId, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("Sign in to merge your cart.");

        if (!TryParseGuestId(guestId, out var parsedGuestId))
            return await GetCartAsync(guestId);

        // EnableRetryOnFailure execution strategies own their transactions: the whole
        // merge (guest rows folded into the user cart) runs inside CreateExecutionStrategy
        // so a transient failure can safely retry the entire unit.
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var guestCart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.GuestId == parsedGuestId.ToString("N") && c.UserId == null, ct);

            if (guestCart is null || guestCart.Items.Count == 0)
            {
                await tx.CommitAsync(ct);
                return await GetCartAsync(guestId);
            }

            var userId = _currentUser.UserId!;
            var userCart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (userCart is null)
            {
                // Adopt the guest cart as the user's cart instead of creating an empty one.
                guestCart.UserId = userId;
                guestCart.GuestId = null;
                guestCart.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return await GetCartAsync(null);
            }

            foreach (var guestItem in guestCart.Items)
            {
                var existing = userCart.Items.FirstOrDefault(i =>
                    i.ProductId == guestItem.ProductId && i.VariantId == guestItem.VariantId);

                if (existing is null)
                {
                    guestItem.CartId = userCart.Id;
                    _db.CartItems.Update(guestItem);
                }
                else
                {
                    var stock = guestItem.VariantId is int vid
                        ? (await _db.ProductVariants.FindAsync(new object[] { vid }, ct))?.Stock ?? 0
                        : (await _db.Products.FindAsync(new object[] { guestItem.ProductId }, ct))?.Stock ?? 0;

                    existing.Quantity = Math.Min(existing.Quantity + guestItem.Quantity, Math.Max(stock, existing.Quantity));
                    existing.UpdatedAt = DateTime.UtcNow;
                    _db.CartItems.Remove(guestItem);
                }
            }

            _db.Carts.Remove(guestCart);
            userCart.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await GetCartAsync(null);
        });
    }

    // ---- internals -------------------------------------------------------

    private static bool TryParseGuestId(string? guestId, out Guid parsed)
        => Guid.TryParse(guestId, out parsed);

    /// <summary>
    /// Resolves the caller's cart with ownership validation on every request.
    /// Returns null only for read access when no cart exists yet.
    /// </summary>
    private async Task<Cart?> ResolveAsync(string? guestId, bool forWrite, CancellationToken ct = default)
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;
            var userCart = await _db.Carts
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .Include(c => c.Items).ThenInclude(i => i.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (userCart is not null)
                return userCart;

            if (!forWrite)
                return null;

            userCart = new Cart { Id = Guid.NewGuid(), UserId = userId };
            _db.Carts.Add(userCart);
            await _db.SaveChangesAsync(ct);
            return userCart;
        }

        if (!TryParseGuestId(guestId, out var guestGuid))
        {
            if (!forWrite)
                return null;
            throw new ConflictException("A valid cart id header (X-Cart-Id) is required.");
        }

        var guestCart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .Include(c => c.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.GuestId == guestGuid.ToString("N") && c.UserId == null, ct);

        if (guestCart is not null)
            return guestCart;

        if (!forWrite)
            return null;

        guestCart = new Cart { Id = Guid.NewGuid(), GuestId = guestGuid.ToString("N") };
        _db.Carts.Add(guestCart);
        await _db.SaveChangesAsync(ct);
        return guestCart;
    }

    private static CartDto EmptyCart(string owner) => new()
    {
        Id = Guid.Empty,
        Owner = owner,
        Items = Array.Empty<CartItemDto>(),
        ItemCount = 0,
        Subtotal = 0,
        EstimatedShipping = 0,
        Total = 0
    };

    private static CartDto Map(Cart cart)
    {
        var items = cart.Items
            .OrderBy(i => i.CreatedAt)
            .Select(i =>
            {
                var unit = Pricing.UnitPrice(i.Product, i.Variant);
                var stock = i.Variant?.Stock ?? i.Product.Stock;
                return new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    VariantId = i.VariantId,
                    Name = i.Product.Name,
                    Slug = i.Product.Slug,
                    Brand = i.Product.Brand,
                    Sku = i.Product.Sku,
                    ImageUrl = i.Product.ImageUrl,
                    VariantName = i.Variant?.Name,
                    UnitPrice = unit,
                    Mrp = i.Product.Price + (i.Variant?.PriceDelta ?? 0m),
                    DiscountPercent = i.Product.DiscountPercent,
                    Quantity = i.Quantity,
                    LineTotal = unit * i.Quantity,
                    Stock = stock,
                    MaxQuantity = Math.Min(Math.Max(stock, 0), Pricing.MaxCartQuantity),
                    IsAvailable = i.Product.IsActive && stock > 0
                };
            })
            .ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var shipping = Pricing.ShippingCost(ShippingMethod.Standard, subtotal);

        return new CartDto
        {
            Id = cart.Id,
            Owner = cart.UserId is null ? "guest" : "user",
            Items = items,
            ItemCount = items.Sum(i => i.Quantity),
            Subtotal = subtotal,
            EstimatedShipping = shipping,
            Total = subtotal + shipping
        };
    }
}
