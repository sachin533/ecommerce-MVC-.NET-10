using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickShop.Api.Domain;

namespace QuickShop.Api.Data.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.Property(c => c.UserId).HasMaxLength(450);
        builder.Property(c => c.GuestId).HasMaxLength(64);

        // At most one cart per user / per guest identifier (partial unique indexes).
        builder.HasIndex(c => c.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(c => c.GuestId).IsUnique().HasFilter("[GuestId] IS NOT NULL");

        builder.HasOne(c => c.User)
            .WithOne()
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        // One line per product (and per product+variant) within a cart.
        // Filtered unique indexes are required because SQL Server treats NULLs as distinct.
        builder.HasIndex(i => new { i.CartId, i.ProductId })
            .IsUnique()
            .HasFilter("[VariantId] IS NULL");
        builder.HasIndex(i => new { i.CartId, i.ProductId, i.VariantId })
            .IsUnique()
            .HasFilter("[VariantId] IS NOT NULL");

        builder.Property(i => i.Quantity).HasDefaultValue(1);

        builder.HasOne(i => i.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a variant (e.g. admin edits options) simply detaches cart lines.
        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.Property(o => o.OrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2);
        builder.Property(o => o.Total).HasPrecision(18, 2);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.ShippingMethod)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(o => o.ShipFullName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.ShipPhone).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ShipLine1).HasMaxLength(300).IsRequired();
        builder.Property(o => o.ShipLine2).HasMaxLength(300);
        builder.Property(o => o.ShipCity).HasMaxLength(120).IsRequired();
        builder.Property(o => o.ShipState).HasMaxLength(120).IsRequired();
        builder.Property(o => o.ShipPostalCode).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ShipCountry).HasMaxLength(120).IsRequired();
        builder.Property(o => o.CustomerNote).HasMaxLength(1000);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => new { o.Status, o.PlacedAt });
        builder.HasIndex(o => o.PlacedAt);

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.Property(i => i.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(i => i.ProductSlug).HasMaxLength(320).IsRequired();
        builder.Property(i => i.Sku).HasMaxLength(64).IsRequired();
        builder.Property(i => i.ProductImageUrl).HasMaxLength(1000);
        builder.Property(i => i.VariantName).HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.ProductId);

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Keep order history if the product is later deleted. SQL Server forbids a
        // second trigger-creating path into OrderItems (Products -> OrderItems via
        // ProductId *and* Products -> ProductVariants -> OrderItems via VariantId),
        // so this FK must be NO ACTION; products are soft-deactivated, never deleted.
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.Provider).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProviderTransactionId).HasMaxLength(128);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(8).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.Property(p => p.CardLast4).HasMaxLength(4);

        builder.HasIndex(p => p.OrderId).IsUnique();
        builder.HasIndex(p => p.ProviderTransactionId);

        builder.HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");

        builder.HasIndex(w => w.UserId).IsUnique();

        builder.HasOne(w => w.User)
            .WithOne()
            .HasForeignKey<Wishlist>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasIndex(i => new { i.WishlistId, i.ProductId }).IsUnique();

        builder.HasOne(i => i.Wishlist)
            .WithMany(w => w.Items)
            .HasForeignKey(i => i.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
