using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;

namespace QuickShop.Api.Services;

/// <summary>Category queries (public) and validated CRUD (admin).</summary>
public class CategoryService
{
    private readonly QuickShopDbContext _db;

    public CategoryService(QuickShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Categories.AsNoTracking();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        var counts = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,
            SortOrder = c.SortOrder,
            ProductCount = counts.GetValueOrDefault(c.Id)
        }).ToList();
    }

    public async Task<CategoryDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, ct)
            ?? throw new NotFoundException("Category");

        var count = await _db.Products.CountAsync(
            p => p.CategoryId == category.Id && p.IsActive, ct);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            ProductCount = count
        };
    }

    // ---- admin -----------------------------------------------------------

    public async Task<CategoryDto> CreateAsync(CategoryUpsertRequest request, CancellationToken ct = default)
    {
        var slug = Text.Slugify(request.Name);
        if (string.IsNullOrEmpty(slug))
            throw new ValidationException("Please correct the errors below.",
                new Dictionary<string, string[]> { ["Name"] = ["Category name is invalid."] });

        if (await _db.Categories.AnyAsync(c => c.Slug == slug, ct))
            throw new ConflictException($"A category named '{request.Name}' already exists.");

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        return await GetAdminAsync(category.Id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryUpsertRequest request, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Category");

        var slug = Text.Slugify(request.Name);
        if (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, ct))
            throw new ConflictException($"A category named '{request.Name}' already exists.");

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.Description = request.Description?.Trim();
        category.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        category.IsActive = request.IsActive;
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetAdminAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Category");

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id, ct);
        if (hasProducts)
            throw new ConflictException("This category still has products. Move or delete them first.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CategoryDto> GetAdminAsync(int id, CancellationToken ct)
    {
        var category = await _db.Categories.AsNoTracking().FirstAsync(c => c.Id == id, ct);
        var count = await _db.Products.CountAsync(p => p.CategoryId == id, ct);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            ProductCount = count
        };
    }
}
