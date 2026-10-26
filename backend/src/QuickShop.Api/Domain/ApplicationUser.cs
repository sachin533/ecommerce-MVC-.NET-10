using Microsoft.AspNetCore.Identity;

namespace QuickShop.Api.Domain;

/// <summary>Application user extending ASP.NET Core Identity.</summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
