using System.ComponentModel.DataAnnotations;

namespace QuickShop.Api.DTOs;

public record AddressDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "India";
    public bool IsDefault { get; init; }
}

public record AddressUpsertRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be 2-200 characters.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; init; } = string.Empty;

    [Required(ErrorMessage = "Address line is required.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "Address line must be 3-300 characters.")]
    public string Line1 { get; init; } = string.Empty;

    [StringLength(300)]
    public string? Line2 { get; init; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(120)]
    public string City { get; init; } = string.Empty;

    [Required(ErrorMessage = "State is required.")]
    [StringLength(120)]
    public string State { get; init; } = string.Empty;

    [Required(ErrorMessage = "PIN code is required.")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "PIN code must be 6 digits.")]
    public string PostalCode { get; init; } = string.Empty;

    [StringLength(120)]
    public string Country { get; init; } = "India";

    public bool IsDefault { get; init; }
}
