using Xunit;

namespace QuickShop.Tests.Infrastructure;

/// <summary>
/// Groups every integration test class onto one shared API host + database.
/// Test classes in a collection run sequentially, so shared state stays
/// deterministic; tests still use per-test users/carts and delta-based
/// assertions (never absolute totals that another test could shift).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationFixture : ICollectionFixture<QuickShopApiFactory>
{
    public const string Name = "integration";
}
