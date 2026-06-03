using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.WebApi.Common;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>
/// 訂單與訂單明細：建立（扣庫存）、查詢、更新（樂觀並行）、取消（還原庫存）。所有端點需 JWT。
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>列出所有訂單（含明細）。</summary>
    /// <param name="ct">取消權杖。</param>
    /// <returns>訂單清單。</returns>
    /// <response code="200">回傳訂單清單。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> List(CancellationToken ct)
        => Ok(await orderService.ListAsync(ct));

    /// <summary>依編號取得單一訂單（含明細）。</summary>
    /// <param name="id">訂單編號。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>指定訂單。</returns>
    /// <response code="200">回傳指定訂單。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定訂單。</response>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken ct)
        => (await orderService.GetAsync(id, ct)).ToOk();

    /// <summary>建立訂單並扣減庫存。</summary>
    /// <remarks>
    /// 每筆明細以條件式 UPDATE 扣庫存；任一產品庫存不足或不存在即整筆 rollback 並回 400（不會部分扣減）。
    /// 訂單建立者取自 JWT 的員工編號；訂單日期由伺服器以 UTC 記錄。
    /// </remarks>
    /// <param name="request">建立請求（客戶編號 + 至少一筆明細）。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>新建立的訂單。</returns>
    /// <response code="201">建立成功，回傳新訂單（含 Location header）。</response>
    /// <response code="400">明細為空、數量 ≤ 0，或庫存不足 / 產品不存在。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, CurrentEmployeeId(), ct);
        return result.ToActionResult(value => CreatedAtAction(nameof(Get), new { id = value.OrderId }, value));
    }

    /// <summary>更新訂單明細（樂觀並行控制）。</summary>
    /// <remarks>
    /// 依明細差異增減庫存：數量增加則再扣庫存（不足回 400），減少則還原；請求中移除的明細還原庫存後刪除。
    /// 每筆明細需帶讀取時的 <c>version</c>，與資料庫不符即回 409（版本衝突）並整筆 rollback。
    /// </remarks>
    /// <param name="id">訂單編號。</param>
    /// <param name="request">更新請求（明細清單，每筆含 version）。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>更新後的訂單。</returns>
    /// <response code="200">更新成功，回傳更新後訂單。</response>
    /// <response code="400">明細為空、數量 ≤ 0，或庫存不足。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定訂單。</response>
    /// <response code="409">訂單已取消，或明細版本衝突。</response>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderDto>> Update(int id, UpdateOrderRequest request, CancellationToken ct)
        => (await orderService.UpdateAsync(id, request, CurrentEmployeeId(), ct)).ToOk();

    /// <summary>取消訂單（軟刪除 <c>is_canceled</c>）並還原庫存。</summary>
    /// <remarks>還原該訂單所有明細的庫存。重複取消為冪等（仍回 204）。已付清（paidoff）的訂單不可取消。</remarks>
    /// <param name="id">訂單編號。</param>
    /// <param name="ct">取消權杖。</param>
    /// <response code="204">取消成功（或已是取消狀態）。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定訂單。</response>
    /// <response code="409">訂單已付清，不可取消。</response>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await orderService.CancelAsync(id, CurrentEmployeeId(), ct);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>取消訂單（與 <c>DELETE /api/orders/{id}</c> 同義的顯式端點）。</summary>
    /// <param name="id">訂單編號。</param>
    /// <param name="ct">取消權杖。</param>
    /// <response code="204">取消成功（或已是取消狀態）。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定訂單。</response>
    /// <response code="409">訂單已付清，不可取消。</response>
    [HttpPost("{id:int}/cancel")]
    public Task<IActionResult> CancelExplicit(int id, CancellationToken ct) => Cancel(id, ct);

    private int CurrentEmployeeId()
        => int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : 0;
}
