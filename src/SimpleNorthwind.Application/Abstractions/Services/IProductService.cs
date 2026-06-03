using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Products;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IProductService
{
    /// <summary>分頁 + 類別過濾 + 排序的產品唯讀清單。</summary>
    Task<PagedResult<ProductDto>> ListAsync(int page, int pageSize, string? category, string? sortBy, bool desc, CancellationToken ct = default);
}
