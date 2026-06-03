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

// Version 為樂觀並行 token：client 帶回讀到的版本供衝突偵測（非可任意修改的資料欄位），server 比對後自增。
public sealed record UpdateOrderDetailRequest(int ProductId, int OrderQuantities, decimal Discount, int Version);

public sealed record UpdateOrderRequest(IReadOnlyList<UpdateOrderDetailRequest> Details);
