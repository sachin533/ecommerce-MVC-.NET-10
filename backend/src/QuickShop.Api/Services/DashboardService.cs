using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;

namespace QuickShop.Api.Services;

/// <summary>Dashboard statistics computed from real database records.</summary>
public class DashboardService
{
    private const int LowStockThreshold = 10;

    private readonly QuickShopDbContext _db;

    public DashboardService(QuickShopDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var revenue = await _db.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total, ct) ?? 0m;

        var totalOrders = await _db.Orders.CountAsync(ct);
        var totalCustomers = await _db.Users.CountAsync(ct);
        var totalProducts = await _db.Products.CountAsync(ct);

        var openOrders = await _db.Orders.CountAsync(o =>
            o.Status == OrderStatus.Pending || o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing, ct);

        var lowStock = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= LowStockThreshold)
            .OrderBy(p => p.Stock)
            .Select(p => new LowStockItemDto(p.Id, p.Name, p.Sku, p.Stock, p.Category!.Name))
            .ToListAsync(ct);

        // Revenue per day for the last 7 days (UTC).
        var cutoff = DateTime.UtcNow.Date.AddDays(-6);
        var daily = await _db.Orders.AsNoTracking()
            .Where(o => o.PlacedAt >= cutoff && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.PlacedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.Total), Orders = g.Count() })
            .ToDictionaryAsync(g => g.Date, g => g, ct);

        var last7 = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-i))
            .Select(day =>
            {
                daily.TryGetValue(day, out var value);
                return new RevenuePointDto(
                    DateOnly.FromDateTime(day),
                    value?.Revenue ?? 0m,
                    value?.Orders ?? 0);
            })
            .Reverse()
            .ToList();

        // Project to an anonymous shape for the server-side ORDER BY (EF cannot
        // translate an ordering over a constructor call), then map to the DTO locally.
        var topCategories = (await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status != OrderStatus.Cancelled && i.ProductId != null)
            .GroupBy(i => i.Product!.Category!.Name)
            .Select(g => new
            {
                Name = g.Key,
                Revenue = g.Sum(i => i.LineTotal),
                Orders = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(c => c.Revenue)
            .Take(5)
            .ToListAsync(ct))
            .Select(c => new CategoryRevenueDto(c.Name, c.Revenue, c.Orders))
            .ToList();

        var recentOrders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderByDescending(o => o.PlacedAt)
            .Take(5)
            .ToListAsync(ct);

        return new DashboardStatsDto
        {
            TotalRevenue = revenue,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            PendingOrders = openOrders,
            LowStockProducts = lowStock.Count,
            RevenueLast7Days = last7,
            TopCategories = topCategories,
            RecentOrders = recentOrders.Select(o => OrderService.ToSummary(o, includeCustomer: true)).ToList(),
            LowStockItems = lowStock.Take(8).ToList()
        };
    }
}
