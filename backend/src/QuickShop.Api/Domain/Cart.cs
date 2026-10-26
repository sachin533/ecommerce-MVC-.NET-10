namespace QuickShop.Api.Domain;

/// <summary>
/// A cart. Either owned by a registered user (<see cref="UserId"/>), or a guest cart
/// identified by an opaque client-generated <see cref="GuestId"/>. Never both, never neither.
/// </summary>
public class Cart
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Opaque client-generated guest identifier (GUID string).</summary>
    public string? GuestId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
