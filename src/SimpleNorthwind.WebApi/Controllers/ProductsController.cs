using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Products;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>產品唯讀檢視（分頁 + 類別過濾 + 欄位排序）。所有端點需 JWT。</summary>
[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>分頁列出產品（含分類名稱）。</summary>
    /// <param name="page">頁碼（從 1 起，預設 1）。</param>
    /// <param name="pageSize">每頁筆數（1–100，預設 10）。</param>
    /// <param name="category">分類名稱過濾（可選）。</param>
    /// <param name="sortBy">排序欄：<c>name</c> / <c>category</c> / <c>price</c> / <c>stock</c>（預設 name）。</param>
    /// <param name="desc">是否降冪（預設否）。</param>
    /// <param name="ct">取消權杖。</param>
    /// <response code="200">回傳分頁產品清單（含 totalCount）。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool desc = false,
        CancellationToken ct = default)
        => Ok(await productService.ListAsync(page, pageSize, category, sortBy, desc, ct));
}
