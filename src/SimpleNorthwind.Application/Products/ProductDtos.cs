namespace SimpleNorthwind.Application.Products;

/// <summary>產品唯讀檢視 DTO。CategoryName 由 JOIN categories 取得（不外露 category_id）。庫存狀態由前端依門檻判定。</summary>
public sealed record ProductDto(
    int ProductId,
    string ProductName,
    string CategoryName,
    int Quantities,
    decimal UnitPrice);
