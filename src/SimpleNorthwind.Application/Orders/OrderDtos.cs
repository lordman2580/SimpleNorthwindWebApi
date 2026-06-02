namespace SimpleNorthwind.Application.Orders;

public sealed record OrderDetailDto(int ProductId, int OrderQuantities, decimal Discount, int Version);

public sealed record OrderDto(
    int OrderId,
    int CustomerId,
    int EmployeeId,
    DateTime OrderDate,
    int? ModifiedEmployeeId,
    DateTime? ModifiedDate,
    bool IsCanceled,
    bool IsPaidoff,
    IReadOnlyList<OrderDetailDto> Details);

public sealed record CreateOrderDetailRequest(int ProductId, int OrderQuantities, decimal Discount);

public sealed record CreateOrderRequest(int CustomerId, IReadOnlyList<CreateOrderDetailRequest> Details);

public sealed record UpdateOrderDetailRequest(int ProductId, int OrderQuantities, decimal Discount, int Version);

public sealed record UpdateOrderRequest(IReadOnlyList<UpdateOrderDetailRequest> Details);
