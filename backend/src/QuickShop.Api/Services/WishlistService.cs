using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;

namespace QuickShop.Api.Services;

/// <summary>Per-user wishlist (auto-created on first use).</summary>
public class WishlistService
{
    private readonly QuickShopDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WishlistService(QuickShopDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId => _currentUser.UserId
        ?? throw new UnauthorizedException("Sign in to continue.");

    public async Task<WishlistDto> GetAsync(CancellationToken ct = default)
    {
        var wishlist = await EnsureWishlistAsync(ct);
        return await ToDtoAsync(wishlist.Id, ct);
    }

    public async Task<WishlistDto> AddAsync(int productId, CancellationToken ct = default)
    {
        var wishlist = await EnsureWishlistAsync(ct);

        var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct);
        if (!exists)
            throw new NotFoundException("Product");

        var alreadySaved = await _db.WishlistItems
            .AnyAsync(i => i.WishlistId == wishlist.Id && i.ProductId == productId, ct);

        if (!alreadySaved)
        {
            _db.WishlistItems.Add(new WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(wishlist.Id, ct);
    }

    public async Task<WishlistDto> RemoveAsync(int productId, CancellationToken ct = default)
    {
        var wishlist = await EnsureWishlistAsync(ct);

        var item = await _db.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlist.Id && i.ProductId == productId, ct);

        if (item is not null)
        {
            _db.WishlistItems.Remove(item);
            await _db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(wishlist.Id, ct);
    }

    private async Task<Wishlist> EnsureWishlistAsync(CancellationToken ct)
    {
        var userId = UserId;
        var wishlist = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wishlist is not null)
            return wishlist;

        wishlist = new Wishlist { UserId = userId, CreatedAt = DateTime.UtcNow };
        _db.Wishlists.Add(wishlist);
        await _db.SaveChangesAsync(ct);
        return wishlist;
    }

    private async Task<WishlistDto> ToDtoAsync(int wishlistId, CancellationToken ct)
    {
        // Two-step query so ordering survives and Category is loaded for the DTO.
        var orderedIds = await _db.WishlistItems.AsNoTracking()
            .Where(i => i.WishlistId == wishlistId && i.Product.IsActive)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ProductId)
            .ToListAsync(ct);

        if (orderedIds.Count == 0)
            return new WishlistDto(Array.Empty<ProductSummaryDto>());

        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => orderedIds.Contains(p.Id))
            .ToListAsync(ct);

        var byId = products.ToDictionary(p => p.Id);
        var ordered = orderedIds.Where(byId.ContainsKey).Select(id => byId[id]);

        return new WishlistDto(ordered.Select(CatalogService.ToSummary).ToList());
    }
}
