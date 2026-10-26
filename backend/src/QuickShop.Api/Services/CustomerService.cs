using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;

namespace QuickShop.Api.Services;

/// <summary>Admin customer directory with order aggregates.</summary>
public class CustomerService
{
    private readonly QuickShopDbContext _db;

    public CustomerService(QuickShopDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Email!.Contains(term) || u.FullName.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var customers = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.CreatedAt,
                OrderCount = _db.Orders.Count(o => o.UserId == u.Id),
                TotalSpent = _db.Orders
                    .Where(o => o.UserId == u.Id && o.Status != OrderStatus.Cancelled)
                    .Sum(o => (decimal?)o.Total) ?? 0m,
                Roles = _db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToList()
            })
            .ToListAsync(ct);

        return PagedResult<CustomerDto>.Create(customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Email = c.Email ?? string.Empty,
            PhoneNumber = c.PhoneNumber,
            CreatedAt = c.CreatedAt,
            OrderCount = c.OrderCount,
            TotalSpent = c.TotalSpent,
            Roles = c.Roles
        }).ToList(), page, pageSize, total);
    }
}
