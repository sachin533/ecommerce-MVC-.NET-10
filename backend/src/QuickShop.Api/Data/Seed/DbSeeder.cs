using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickShop.Api.Domain;

namespace QuickShop.Api.Data.Seed;

public record SeedReport(int CategoriesAdded, int ProductsAdded, int AccountsCreated, int OrdersAdded);

/// <summary>
/// Idempotent DEVELOPMENT seeder: inserts missing roles, categories, products,
/// demo accounts and sample orders from an embedded dataset. Existing rows are
/// never modified, so running it repeatedly (or against a database with real
/// data) cannot overwrite anything.
/// </summary>
public static class DbSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<SeedReport> SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<QuickShopDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        var dataset = LoadDataset();

        var categoriesAdded = await SeedCategoriesAsync(db, dataset.Categories, ct);
        var productsAdded = await SeedProductsAsync(db, dataset.Products, ct);
        var accountsCreated = await SeedAccountsAsync(db, userManager, dataset.Accounts, ct);
        var ordersAdded = await SeedOrdersAsync(db, dataset.Orders, ct);

        if (accountsCreated > 0)
            await EnsureRolesAsync(roleManager, ct);

        logger.LogInformation(
            "Seed complete — +{Categories} categories, +{Products} products, +{Accounts} accounts, +{Orders} orders",
            categoriesAdded, productsAdded, accountsCreated, ordersAdded);

        return new SeedReport(categoriesAdded, productsAdded, accountsCreated, ordersAdded);
    }

    private static SeedDataset LoadDataset()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{typeof(QuickShopDbContext).Namespace}.Seed.seed-data.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed dataset '{resourceName}' not found.");

        return JsonSerializer.Deserialize<SeedDataset>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Seed dataset is empty or malformed.");
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, CancellationToken ct)
    {
        foreach (var role in new[] { "Customer", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<int> SeedCategoriesAsync(QuickShopDbContext db, List<SeedCategory> categories, CancellationToken ct)
    {
        var existing = (await db.Categories.Select(c => c.Slug).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var category in categories)
        {
            if (existing.Contains(category.Slug))
                continue;

            db.Categories.Add(new Category
            {
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = true,
                SortOrder = category.SortOrder,
                CreatedAt = DateTime.UtcNow.AddDays(-150)
            });
            existing.Add(category.Slug);
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(ct);

        return added;
    }

    private static async Task<int> SeedProductsAsync(QuickShopDbContext db, List<SeedProduct> products, CancellationToken ct)
    {
        var categoryBySlug = (await db.Categories.ToListAsync(ct))
            .ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);

        var existingSkus = (await db.Products.Select(p => p.Sku).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingSlugs = (await db.Products.Select(p => p.Slug).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var seed in products)
        {
            if (existingSkus.Contains(seed.Sku))
                continue;

            if (!categoryBySlug.TryGetValue(seed.CategorySlug, out var category))
            {
                // Never create orphaned products — categories are seeded first.
                continue;
            }

            var slug = seed.Slug ?? Slugify(seed.Name);
            var suffix = 2;
            while (existingSlugs.Contains(slug))
                slug = $"{Slugify(seed.Name)}-{suffix++}";

            var product = new Product
            {
                Name = seed.Name,
                Slug = slug,
                Sku = seed.Sku.ToUpperInvariant(),
                Brand = seed.Brand,
                Description = seed.Description,
                Price = seed.Price,
                DiscountPercent = seed.DiscountPercent,
                Stock = seed.Stock,
                Rating = seed.Rating,
                RatingCount = seed.RatingCount,
                CategoryId = category.Id,
                ImageUrl = seed.Images.FirstOrDefault() ?? string.Empty,
                IsFeatured = seed.IsFeatured,
                IsActive = seed.IsActive,
                CreatedAt = DateTime.UtcNow.AddDays(-Math.Clamp(seed.CreatedAtDaysAgo, 1, 365)),
                UpdatedAt = DateTime.UtcNow.AddDays(-Math.Clamp(seed.CreatedAtDaysAgo, 1, 365))
            };

            for (var i = 1; i < seed.Images.Count; i++)
            {
                product.Images.Add(new ProductImage { Url = seed.Images[i], SortOrder = i });
            }
            if (product.Images.Count == 0 && !string.IsNullOrEmpty(product.ImageUrl))
            {
                product.Images.Add(new ProductImage { Url = product.ImageUrl, SortOrder = 0 });
            }

            foreach (var variant in seed.Variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    Name = variant.Name,
                    PriceDelta = variant.PriceDelta,
                    Stock = variant.Stock,
                    IsActive = true,
                    CreatedAt = product.CreatedAt
                });
            }

            db.Products.Add(product);
            existingSkus.Add(product.Sku);
            existingSlugs.Add(slug);
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(ct);

        return added;
    }

    private static async Task<int> SeedAccountsAsync(
        QuickShopDbContext db,
        UserManager<ApplicationUser> userManager,
        SeedAccounts accounts,
        CancellationToken ct)
    {
        var created = 0;

        async Task<ApplicationUser> EnsureUserAsync(SeedAccount seed)
        {
            var existing = await userManager.FindByEmailAsync(seed.Email);
            if (existing is not null)
                return existing;

            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                EmailConfirmed = true,
                FullName = seed.FullName
            };

            // Development-only credentials, documented in README.md.
            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed account {seed.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            created++;
            return user;
        }

        foreach (var role in new[] { "Customer", "Admin" })
        {
            if (!await db.Roles.AnyAsync(r => r.Name == role, ct))
                db.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpperInvariant() });
        }
        await db.SaveChangesAsync(ct);

        var admin = await EnsureUserAsync(accounts.Admin);
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
        if (!await userManager.IsInRoleAsync(admin, "Customer"))
            await userManager.AddToRoleAsync(admin, "Customer");

        var addressesByUser = new Dictionary<string, List<SeedAddress>>();

        foreach (var seed in accounts.Customers)
        {
            var user = await EnsureUserAsync(seed);
            if (!await userManager.IsInRoleAsync(user, "Customer"))
                await userManager.AddToRoleAsync(user, "Customer");

            addressesByUser[user.Id] = seed.Addresses;
        }

        // Attach demo addresses (idempotent per user).
        foreach (var (userId, addresses) in addressesByUser)
        {
            if (await db.Addresses.AnyAsync(a => a.UserId == userId, ct))
                continue;

            for (var i = 0; i < addresses.Count; i++)
            {
                var source = addresses[i];
                db.Addresses.Add(new Address
                {
                    UserId = userId,
                    FullName = source.FullName,
                    Phone = source.Phone,
                    Line1 = source.Line1,
                    Line2 = source.Line2,
                    City = source.City,
                    State = source.State,
                    PostalCode = source.PostalCode,
                    Country = source.Country,
                    IsDefault = i == 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return created;
    }

    private static async Task<int> SeedOrdersAsync(QuickShopDbContext db, List<SeedOrder> orders, CancellationToken ct)
    {
        // Only seed sample orders into a database that has none — never over real orders.
        if (await db.Orders.AnyAsync(ct))
            return 0;

        var products = await db.Products.Include(p => p.Category).ToListAsync(ct);
        var bySku = products.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);

        var users = await db.Users.ToListAsync(ct);
        var byEmail = users.Where(u => u.Email != null).ToDictionary(u => u.Email!, StringComparer.OrdinalIgnoreCase);

        var addresses = await db.Addresses.ToListAsync(ct);
        var added = 0;
        var index = 0;

        foreach (var seed in orders)
        {
            if (!byEmail.TryGetValue(seed.CustomerEmail, out var user))
                continue;

            var lines = new List<(SeedOrderLine Line, Product Product)>();
            foreach (var line in seed.Lines)
            {
                if (bySku.TryGetValue(line.Sku, out var product))
                    lines.Add((line, product));
            }

            if (lines.Count == 0)
                continue;

            index++;

            var address = addresses.FirstOrDefault(a => a.UserId == user.Id && a.IsDefault)
                          ?? addresses.FirstOrDefault(a => a.UserId == user.Id);

            var method = seed.ShippingMethod == "express" ? ShippingMethod.Express : ShippingMethod.Standard;
            var subtotal = lines.Sum(l =>
                Pricing.UnitPrice(l.Product, null) * l.Line.Quantity);
            var shipping = Pricing.ShippingCost(method, subtotal);
            var status = Enum.TryParse<OrderStatus>(seed.Status, out var parsed) ? parsed : OrderStatus.Delivered;

            var order = new Order
            {
                OrderNumber = $"ORD{index:D7}",
                UserId = user.Id,
                Status = status,
                Subtotal = subtotal,
                ShippingCost = shipping,
                Total = subtotal + shipping,
                ShippingMethod = method,
                PaymentMethod = "mock_card",
                ShipFullName = address?.FullName ?? user.FullName,
                ShipPhone = address?.Phone ?? "9876543210",
                ShipLine1 = address?.Line1 ?? "221B Demo Street",
                ShipLine2 = address?.Line2,
                ShipCity = address?.City ?? "Bengaluru",
                ShipState = address?.State ?? "Karnataka",
                ShipPostalCode = address?.PostalCode ?? "560001",
                ShipCountry = address?.Country ?? "India",
                PlacedAt = DateTime.UtcNow.AddDays(-Math.Clamp(seed.DaysAgo, 1, 90)),
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var (line, product) in lines)
            {
                var unit = Pricing.UnitPrice(product, null);
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSlug = product.Slug,
                    Sku = product.Sku,
                    ProductImageUrl = product.ImageUrl,
                    UnitPrice = unit,
                    Quantity = line.Quantity,
                    LineTotal = unit * line.Quantity
                });
            }

            var paymentSucceeded = status != OrderStatus.Cancelled;
            order.Payment = new Payment
            {
                Provider = "mock",
                ProviderTransactionId = paymentSucceeded ? $"MOCK-SEED-{index:D5}" : null,
                Amount = order.Total,
                Currency = "INR",
                Status = paymentSucceeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
                FailureReason = paymentSucceeded ? null : "Simulated provider decline (seed data).",
                CardLast4 = seed.CardLast4,
                CreatedAt = order.PlacedAt,
                ProcessedAt = order.PlacedAt
            };

            db.Orders.Add(order);
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(ct);

        return added;
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(c =>
            char.IsLetterOrDigit(c) ? c : (c is ' ' or '-' or '_' ? '-' : '\0')).ToArray();
        var slug = new string(chars);
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}

// ---------------------------------------------------------------------------
// JSON dataset contract
// ---------------------------------------------------------------------------

internal sealed class SeedDataset
{
    public List<SeedCategory> Categories { get; set; } = new();
    public List<SeedProduct> Products { get; set; } = new();
    public SeedAccounts Accounts { get; set; } = new();
    public List<SeedOrder> Orders { get; set; } = new();
}

internal sealed class SeedCategory
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
}

internal sealed class SeedProduct
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public int Stock { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public string CategorySlug { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedAtDaysAgo { get; set; } = 30;
    public List<string> Images { get; set; } = new();
    public List<SeedVariant> Variants { get; set; } = new();
}

internal sealed class SeedVariant
{
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public int Stock { get; set; }
}

internal sealed class SeedAccounts
{
    public SeedAccount Admin { get; set; } = new();
    public List<SeedAccount> Customers { get; set; } = new();
}

internal sealed class SeedAccount
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<SeedAddress> Addresses { get; set; } = new();
}

internal sealed class SeedAddress
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    [JsonPropertyName("country")]
    public string Country { get; set; } = "India";
}

internal sealed class SeedOrder
{
    public string CustomerEmail { get; set; } = string.Empty;
    public int DaysAgo { get; set; }
    public string Status { get; set; } = "Delivered";
    public string ShippingMethod { get; set; } = "standard";
    public string CardLast4 { get; set; } = "4242";
    public List<SeedOrderLine> Lines { get; set; } = new();
}

internal sealed class SeedOrderLine
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}
