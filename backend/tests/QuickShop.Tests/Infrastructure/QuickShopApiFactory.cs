using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickShop.Api.Data;
using QuickShop.Api.Data.Seed;
using Xunit;

namespace QuickShop.Tests.Infrastructure;

/// <summary>
/// Boots the real API (Program) against an isolated test database on the same SQL
/// Server instance. A throwaway JWT signing key is generated per run so tests never
/// depend on — or expose — the development secret. The database is recreated and
/// seeded exactly once per test run (xUnit collection-fixture lifetime), then shared
/// by every integration test class, which run sequentially in that collection.
/// </summary>
public sealed class QuickShopApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestConnectionString =
        @"Server=.\SQLEXPRESS;Database=QuickShopDb_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    // Random per-run key: valid for the lifetime of one test run, never reused, never real.
    private static readonly string TestJwtKey =
        Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", TestConnectionString);
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Data:ApplyMigrations", "false");
        builder.UseSetting("Data:Seed", "false");
        builder.UseSetting("Auth:SecureCookies", "false");
    }

    /// <summary>Recreates the schema and loads the idempotent dev dataset (186 products, 14 categories, demo accounts).</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuickShopDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }

    Task IAsyncLifetime.InitializeAsync() => ResetDatabaseAsync();

    Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }
}
