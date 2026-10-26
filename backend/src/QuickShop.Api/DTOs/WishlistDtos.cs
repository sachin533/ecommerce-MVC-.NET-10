namespace QuickShop.Api.DTOs;

public record WishlistDto(IReadOnlyList<ProductSummaryDto> Items);
