namespace SimpleNorthwind.Application.Orders;

// 明細：ProductName / UnitPrice 由 JOIN products 取得（前端顯示名稱與金額；小計由前端 = UnitPrice×Qty×(1-Discount/100)）。
public sealed record OrderDetailDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int OrderQuantities,
    decimal Discount,
    int Version);

// 訂單輸出：CustomerName / EmployeeName / ModifiedEmployeeName 由 JOIN 取得；Status 由 is_* 推導（UD9）。
public sealed record OrderDto(
    int OrderId,
    int CustomerId,
    string CustomerName,
    int EmployeeId,
    string EmployeeName,
    DateTime OrderDate,
    int? ModifiedEmployeeId,
    string? ModifiedEmployeeName,
    DateTime? ModifiedDate,
    bool IsCanceled,
    bool IsPaidoff,
    string Status,
    IReadOnlyList<OrderDetailDto> Details);

public sealed record CreateOrderDetailRequest(int ProductId, int OrderQuantities, decimal Discount);

public sealed record CreateOrderRequest(int CustomerId, IReadOnlyList<CreateOrderDetailRequest> Details);

// Version 為樂觀並行 token：client 帶回讀到的版本供衝突偵測（非可任意修改的資料欄位），server 比對後自增。
public sealed record UpdateOrderDetailRequest(int ProductId, int OrderQuantities, decimal Discount, int Version);

public sealed record UpdateOrderRequest(IReadOnlyList<UpdateOrderDetailRequest> Details);

// --- 內部 enrich 讀模型：repository 以 JOIN 投影，OrderService 組為 OrderDto（避免 N+1） ---

public sealed record OrderHeaderRow(
    int OrderId,
    int CustomerId,
    string CustomerName,
    int EmployeeId,
    string EmployeeName,
    DateTime OrderDate,
    int? ModifiedEmployeeId,
    string? ModifiedEmployeeName,
    DateTime? ModifiedDate,
    bool IsCanceled,
    bool IsPaidoff);

public sealed record OrderDetailRow(
    int OrderId,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int OrderQuantities,
    decimal Discount,
    int Version);
