using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Products;

namespace SimpleNorthwind.Application.Services;

/// <summary>產品唯讀查詢（薄封裝；分頁 / 排序 / 過濾邏輯在 repository 的單一 SQL）。</summary>
public sealed class ProductService(IProductRepository products) : IProductService
{
    public Task<PagedResult<ProductDto>> ListAsync(int page, int pageSize, string? category, string? sortBy, bool desc, CancellationToken ct = default) =>
        products.ListAsync(page, pageSize, category, sortBy, desc, ct);
}
