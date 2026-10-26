using System.ComponentModel.DataAnnotations;
using QuickShop.Api.Common;

namespace QuickShop.Api.DTOs;

public record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
}

public record CategoryUpsertRequest(
    [property: Required(ErrorMessage = "Category name is required.")]
    [property: StringLength(120, MinimumLength = 2, ErrorMessage = "Name must be 2-120 characters.")]
    string Name,

    [property: StringLength(1000, ErrorMessage = "Description is too long.")]
    string? Description,

    [property: StringLength(1000)]
    string? ImageUrl,

    bool IsActive = true,

    int SortOrder = 0);
