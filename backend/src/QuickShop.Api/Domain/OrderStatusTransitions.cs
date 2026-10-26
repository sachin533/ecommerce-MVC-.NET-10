namespace QuickShop.Api.Domain;

/// <summary>Allowed order status transitions (business rule).</summary>
public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => from == to || Allowed[from].Contains(to);

    public static IReadOnlyCollection<OrderStatus> NextStates(OrderStatus from) => Allowed[from];
}
