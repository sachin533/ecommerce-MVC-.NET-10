using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;

namespace QuickShop.Api.Services;

/// <summary>Address book CRUD, always scoped to the authenticated user.</summary>
public class AddressService
{
    private readonly QuickShopDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddressService(QuickShopDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string UserId => _currentUser.UserId
        ?? throw new UnauthorizedException("Sign in to continue.");

    public async Task<IReadOnlyList<AddressDto>> ListAsync(CancellationToken ct = default)
    {
        var userId = UserId;
        var addresses = await _db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return addresses.Select(ToDto).ToList();
    }

    public async Task<AddressDto> CreateAsync(AddressUpsertRequest request, CancellationToken ct = default)
    {
        var userId = UserId;
        var isFirst = !await _db.Addresses.AnyAsync(a => a.UserId == userId, ct);

        if (request.IsDefault || isFirst)
            await ResetDefaultsAsync(userId, ct);

        var address = new Address
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
            IsDefault = request.IsDefault || isFirst,
            CreatedAt = DateTime.UtcNow
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<AddressDto> UpdateAsync(int id, AddressUpsertRequest request, CancellationToken ct = default)
    {
        var userId = UserId;
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address");

        if (request.IsDefault && !address.IsDefault)
            await ResetDefaultsAsync(userId, ct);

        address.FullName = request.FullName.Trim();
        address.Phone = request.Phone.Trim();
        address.Line1 = request.Line1.Trim();
        address.Line2 = request.Line2?.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim();
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<AddressDto> SetDefaultAsync(int id, CancellationToken ct = default)
    {
        var userId = UserId;
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address");

        await ResetDefaultsAsync(userId, ct);
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ToDto(address);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var userId = UserId;
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Address");

        var wasDefault = address.IsDefault;
        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync(ct);

        if (wasDefault)
        {
            var next = await _db.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (next is not null)
            {
                next.IsDefault = true;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task ResetDefaultsAsync(string userId, CancellationToken ct)
    {
        var defaults = await _db.Addresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(ct);

        foreach (var address in defaults)
            address.IsDefault = false;
    }

    private static AddressDto ToDto(Address a) => new()
    {
        Id = a.Id,
        FullName = a.FullName,
        Phone = a.Phone,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        IsDefault = a.IsDefault
    };
}
