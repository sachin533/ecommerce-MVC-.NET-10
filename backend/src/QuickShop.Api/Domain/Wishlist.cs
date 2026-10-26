namespace QuickShop.Api.Domain;

/// <summary>A user's wishlist (one per user).</summary>
public class Wishlist
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}

/// <summary>A product saved to a wishlist.</summary>
public class WishlistItem
{
    public int Id { get; set; }

    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
