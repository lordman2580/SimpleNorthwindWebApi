using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.WebApi.Common;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>客戶主檔 CRUD。所有端點需 JWT。</summary>
[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    /// <summary>列出所有客戶。</summary>
    /// <param name="ct">取消權杖。</param>
    /// <returns>客戶清單。</returns>
    /// <response code="200">回傳客戶清單。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(CancellationToken ct)
        => Ok(await customerService.ListAsync(ct));

    /// <summary>依編號取得單一客戶。</summary>
    /// <param name="id">客戶編號。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>指定客戶。</returns>
    /// <response code="200">回傳指定客戶。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定客戶。</response>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Get(int id, CancellationToken ct)
        => (await customerService.GetAsync(id, ct)).ToOk();

    /// <summary>建立客戶。</summary>
    /// <remarks>建立者（<c>create_user</c>）取自 JWT；建立時間以 UTC 記錄、輸出時轉為呼叫端時區。</remarks>
    /// <param name="request">建立請求（公司名稱必填）。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>新建立的客戶。</returns>
    /// <response code="201">建立成功，回傳新客戶（含 Location header）。</response>
    /// <response code="400">請求格式不符（如公司名稱空白）。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.CreateAsync(request, CurrentUser(), ct);
        return result.ToActionResult(value => CreatedAtAction(nameof(Get), new { id = value.CustomerId }, value));
    }

    /// <summary>更新客戶。</summary>
    /// <remarks>日期欄（如 <c>outContactedDate</c>）以呼叫端時區輸入、轉為 UTC 儲存。</remarks>
    /// <param name="id">客戶編號。</param>
    /// <param name="request">更新請求。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>更新後的客戶。</returns>
    /// <response code="200">更新成功，回傳更新後客戶。</response>
    /// <response code="400">請求格式不符。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定客戶。</response>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, UpdateCustomerRequest request, CancellationToken ct)
        => (await customerService.UpdateAsync(id, request, ct)).ToOk();

    /// <summary>刪除客戶。</summary>
    /// <param name="id">客戶編號。</param>
    /// <param name="ct">取消權杖。</param>
    /// <response code="204">刪除成功。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    /// <response code="404">找不到指定客戶。</response>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await customerService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    private string CurrentUser()
        => User.FindFirst("name")?.Value
           ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
           ?? "unknown";
}
