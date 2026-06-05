using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Products;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// 產品清單（唯讀，server-side 分頁）UI controller。
/// 透過 loopback client 取得分頁產品資料，並另抓一批以彙整分類 chip 清單（best-effort）。
/// 401 由 <see cref="UiControllerBase"/> 統一導回登入頁。
/// </summary>
[Route("products")]
public sealed class ProductsUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    private const string ViewPath = "~/Web/Views/Products/Index.cshtml";

    /// <summary>產品清單：依分類 / 排序 / 分頁查詢，並彙整可用分類供 chip 列。</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? category,
        string? sortBy,
        bool desc = false,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        // 正規化排序欄位（容錯：未知值回退為 name）。
        var normalizedSort = NormalizeSort(sortBy);

        // 先彙整分類 chip 清單（best-effort，失敗回空清單）。
        var categories = await LoadCategoriesAsync(ct);

        var result = await apiClient.ListProductsAsync(page, pageSize, category, normalizedSort, desc, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Detail ?? "載入產品清單失敗。";
            return View(ViewPath, new ProductIndexViewModel
            {
                Page = PagedViewModel<ProductViewModel>.From([], page, pageSize, 0),
                Category = category,
                SortBy = normalizedSort,
                Desc = desc,
                Categories = categories,
            });
        }

        var paged = result.Value;
        var items = paged.Items.Select(MapToVm).ToList();

        var vm = new ProductIndexViewModel
        {
            Page = PagedViewModel<ProductViewModel>.From(items, paged.Page, paged.PageSize, paged.TotalCount),
            Category = category,
            SortBy = normalizedSort,
            Desc = desc,
            Categories = categories,
        };
        return View(ViewPath, vm);
    }

    /// <summary>彙整可用分類名稱（單抓一批以分類排序，取 distinct；失敗回空）。</summary>
    private async Task<IReadOnlyList<string>> LoadCategoriesAsync(CancellationToken ct)
    {
        var result = await apiClient.ListProductsAsync(1, 100, null, "category", false, ct);
        if (!result.IsSuccess || result.Value is null)
            return [];

        return result.Value.Items
            .Select(p => p.CategoryName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>正規化排序欄位：僅接受 name / category / price / stock，其餘回退 name。</summary>
    private static string NormalizeSort(string? sortBy)
        => sortBy switch
        {
            "category" => "category",
            "price" => "price",
            "stock" => "stock",
            _ => "name",
        };

    /// <summary>DTO → 檢視模型：庫存為 0 標記為「缺貨」。</summary>
    private static ProductViewModel MapToVm(ProductDto dto)
        => new()
        {
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            CategoryName = dto.CategoryName,
            Quantities = dto.Quantities,
            UnitPrice = dto.UnitPrice,
            StockStatus = dto.Quantities == 0 ? "缺貨" : "正常",
        };
}
