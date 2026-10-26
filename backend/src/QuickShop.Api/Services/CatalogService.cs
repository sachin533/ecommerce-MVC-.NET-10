using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;

namespace QuickShop.Api.Services;

/// <summary>
/// Product catalog: server-side search/filter/sort/pagination for the storefront,
/// plus validated CRUD used by the admin endpoints.
/// </summary>
public class CatalogService
{
    private readonly QuickShopDbContext _db;

    public CatalogService(QuickShopDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Pagination.Normalize(query.Page ?? 1, query.PageSize ?? 12);
        var q = BuildQuery(query);

        var total = await q.CountAsync(ct);

        q = ApplySort(q, query.Sort);

        var items = await q
            .Include(p => p.Category)
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<ProductSummaryDto>.Create(items.Select(ToSummary).ToList(), page, pageSize, total);
    }

    public async Task<ProductDetailDto> GetDetailAsync(int id, bool includeInactive = false, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Product");

        if (!product.IsActive && !includeInactive)
            throw new NotFoundException("Product");

        return ToDetail(product);
    }

    public async Task<ProductDetailDto> GetDetailBySlugAsync(string slug, bool includeInactive = false, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct)
            ?? throw new NotFoundException("Product");

        if (!product.IsActive && !includeInactive)
            throw new NotFoundException("Product");

        return ToDetail(product);
    }

    public async Task<IReadOnlyList<ProductSummaryDto>> FeaturedAsync(int take = 8, CancellationToken ct = default)
    {
        var items = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.RatingCount)
            .ThenByDescending(p => p.Id)
            .Take(Math.Clamp(take, 1, 24))
            .ToListAsync(ct);

        return items.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<ProductSummaryDto>> RelatedAsync(int productId, int take = 4, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("Product");

        var items = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Id != productId && p.CategoryId == product.CategoryId)
            .OrderByDescending(p => p.Rating)
            .Take(Math.Clamp(take, 1, 12))
            .ToListAsync(ct);

        return items.Select(ToSummary).ToList();
    }

    /// <summary>Facets (brands, price range, category counts) for the filter sidebar.</summary>
    public async Task<ProductFiltersDto> FiltersAsync(string? categorySlug = null, CancellationToken ct = default)
    {
        var baseQuery = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var categoryId = await _db.Categories
                .Where(c => c.Slug == categorySlug)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);
            baseQuery = baseQuery.Where(p => p.CategoryId == categoryId);
        }

        var brands = await baseQuery
            .Select(p => p.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync(ct);

        var prices = await baseQuery
            .Select(p => new { p.Price, p.DiscountPercent })
            .ToListAsync(ct);

        var effective = prices.Select(p => Pricing.EffectivePrice(p.Price, p.DiscountPercent)).ToList();
        var range = effective.Count > 0
            ? new PriceRangeDto(effective.Min(), effective.Max())
            : new PriceRangeDto(0, 0);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryFacetDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Products.Count(p => p.IsActive)))
            .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);

        return new ProductFiltersDto(brands, range, categories, total);
    }

    // ---- admin CRUD ------------------------------------------------------

    public async Task<ProductDetailDto> CreateProductAsync(AdminProductUpsertRequest request, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new ValidationException("Please correct the errors below.",
                new Dictionary<string, string[]> { ["CategoryId"] = ["The selected category does not exist."] });

        var slug = Text.Slugify(request.Name);
        if (await _db.Products.AnyAsync(p => p.Slug == slug, ct))
            throw new ConflictException("A product with this name already exists.");

        var sku = string.IsNullOrWhiteSpace(request.Sku)
            ? await GenerateSkuAsync(request.Brand, slug, ct)
            : request.Sku.Trim().ToUpperInvariant();

        if (await _db.Products.AnyAsync(p => p.Sku == sku, ct))
            throw new ConflictException($"SKU '{sku}' is already in use.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Sku = sku,
            Brand = request.Brand.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            DiscountPercent = request.DiscountPercent,
            Stock = request.Stock,
            Rating = request.Rating,
            RatingCount = request.RatingCount,
            CategoryId = category.Id,
            ImageUrl = request.ImageUrl.Trim(),
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        AddImages(product, request.ImageUrl, request.Images);
        AddVariants(product, request.Variants);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(product.Id, includeInactive: true, ct);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(int id, AdminProductUpsertRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Product");

        _ = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new ValidationException("Please correct the errors below.",
                new Dictionary<string, string[]> { ["CategoryId"] = ["The selected category does not exist."] });

        var slug = Text.Slugify(request.Name);
        if (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != id, ct))
            throw new ConflictException("A product with this name already exists.");

        var sku = string.IsNullOrWhiteSpace(request.Sku)
            ? product.Sku
            : request.Sku.Trim().ToUpperInvariant();

        if (sku != product.Sku && await _db.Products.AnyAsync(p => p.Sku == sku && p.Id != id, ct))
            throw new ConflictException($"SKU '{sku}' is already in use.");

        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.Sku = sku;
        product.Brand = request.Brand.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.DiscountPercent = request.DiscountPercent;
        product.Stock = request.Stock;
        product.Rating = request.Rating;
        product.RatingCount = request.RatingCount;
        product.CategoryId = request.CategoryId;
        product.ImageUrl = request.ImageUrl.Trim();
        product.IsFeatured = request.IsFeatured;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        // Replace gallery.
        _db.ProductImages.RemoveRange(product.Images);
        product.Images.Clear();
        AddImages(product, request.ImageUrl, request.Images);

        // Replace variants (cart lines referencing removed variants are detached by FK).
        if (product.Variants.Count > 0)
        {
            _db.ProductVariants.RemoveRange(product.Variants);
            product.Variants.Clear();
        }
        AddVariants(product, request.Variants);

        await _db.SaveChangesAsync(ct);
        return await GetDetailAsync(product.Id, includeInactive: true, ct);
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Product");

        // Cart lines can't survive their product (restrict FK) — remove them first.
        var cartLines = await _db.CartItems.Where(i => i.ProductId == id).ToListAsync(ct);
        _db.CartItems.RemoveRange(cartLines);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductDetailDto> SetStockAsync(int id, int stock, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Product");

        product.Stock = stock;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(id, includeInactive: true, ct);
    }

    // ---- helpers ---------------------------------------------------------

    private IQueryable<Product> BuildQuery(ProductQuery query)
    {
        var q = _db.Products.AsQueryable();

        if (query.IncludeInactive != true)
            q = q.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(term) || p.Brand.Contains(term));
        }

        if (query.CategoryId is int categoryId)
            q = q.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            q = q.Where(p => p.Category.Slug == query.CategorySlug);

        if (!string.IsNullOrWhiteSpace(query.Brand))
        {
            var brand = query.Brand.Trim();
            q = q.Where(p => p.Brand == brand);
        }

        if (query.MinPrice is decimal min)
            q = q.Where(p => p.Price * (100m - p.DiscountPercent) / 100m >= min);

        if (query.MaxPrice is decimal max)
            q = q.Where(p => p.Price * (100m - p.DiscountPercent) / 100m <= max);

        if (query.InStockOnly == true)
            q = q.Where(p => p.Stock > 0);

        if (query.Featured == true)
            q = q.Where(p => p.IsFeatured);

        return q;
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> q, string? sort) => (sort ?? "newest").ToLowerInvariant() switch
    {
        "price-asc" => q.OrderBy(p => p.Price * (100m - p.DiscountPercent) / 100m).ThenBy(p => p.Id),
        "price-desc" => q.OrderByDescending(p => p.Price * (100m - p.DiscountPercent) / 100m).ThenBy(p => p.Id),
        "rating" => q.OrderByDescending(p => p.Rating).ThenByDescending(p => p.RatingCount),
        "popularity" => q.OrderByDescending(p => p.RatingCount).ThenByDescending(p => p.Rating),
        _ => q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
    };

    private static void AddImages(Product product, string primaryUrl, List<string>? extra)
    {
        var sort = 0;
        product.Images.Add(new ProductImage { ProductId = product.Id, Url = primaryUrl.Trim(), SortOrder = sort++ });

        foreach (var url in (extra ?? new List<string>())
                     .Where(u => !string.IsNullOrWhiteSpace(u))
                     .Select(u => u.Trim())
                     .Where(u => !string.Equals(u, primaryUrl.Trim(), StringComparison.OrdinalIgnoreCase))
                     .Take(8))
        {
            product.Images.Add(new ProductImage { ProductId = product.Id, Url = url, SortOrder = sort++ });
        }
    }

    private static void AddVariants(Product product, List<VariantUpsertRequest>? variants)
    {
        foreach (var v in (variants ?? new List<VariantUpsertRequest>()).Take(10))
        {
            product.Variants.Add(new ProductVariant
            {
                ProductId = product.Id,
                Name = v.Name.Trim(),
                PriceDelta = v.PriceDelta,
                Stock = v.Stock,
                IsActive = v.IsActive,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<string> GenerateSkuAsync(string brand, string slug, CancellationToken ct)
    {
        var prefix = Text.Slugify(brand).Replace("-", string.Empty);
        if (prefix.Length > 4) prefix = prefix[..4];

        for (var attempt = 0; attempt < 25; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var candidate = $"{prefix.ToUpperInvariant()}-{suffix}";
            if (!await _db.Products.AnyAsync(p => p.Sku == candidate, ct))
                return candidate;
        }

        return $"SKU-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
    }

    // ---- mapping ---------------------------------------------------------

    public static ProductSummaryDto ToSummary(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Sku = p.Sku,
        Brand = p.Brand,
        Price = p.Price,
        DiscountPercent = p.DiscountPercent,
        SalePrice = Pricing.EffectivePrice(p.Price, p.DiscountPercent),
        ImageUrl = p.ImageUrl,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        CategorySlug = p.Category?.Slug ?? string.Empty,
        Rating = p.Rating,
        RatingCount = p.RatingCount,
        Stock = p.Stock,
        IsAvailable = p.IsAvailable,
        IsFeatured = p.IsFeatured,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };

    public static ProductDetailDto ToDetail(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Sku = p.Sku,
        Brand = p.Brand,
        Price = p.Price,
        DiscountPercent = p.DiscountPercent,
        SalePrice = Pricing.EffectivePrice(p.Price, p.DiscountPercent),
        ImageUrl = p.ImageUrl,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        CategorySlug = p.Category?.Slug ?? string.Empty,
        Rating = p.Rating,
        RatingCount = p.RatingCount,
        Stock = p.Stock,
        IsAvailable = p.IsAvailable,
        IsFeatured = p.IsFeatured,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        Description = p.Description,
        Images = p.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageDto(i.Id, i.Url, i.SortOrder))
            .ToList(),
        Variants = p.Variants
            .Where(v => v.IsActive)
            .Select(v => new ProductVariantDto(
                v.Id,
                v.Name,
                v.PriceDelta,
                Pricing.UnitPrice(p, v),
                v.Stock,
                p.IsActive && v.Stock > 0))
            .ToList()
    };
}
