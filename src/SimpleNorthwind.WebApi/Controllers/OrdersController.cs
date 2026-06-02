using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.WebApi.Common;

namespace SimpleNorthwind.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> List(CancellationToken ct)
        => Ok(await orderService.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken ct)
        => (await orderService.GetAsync(id, ct)).ToOk();

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orderService.CreateAsync(request, CurrentEmployeeId(), ct);
        return result.ToActionResult(value => CreatedAtAction(nameof(Get), new { id = value.OrderId }, value));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderDto>> Update(int id, UpdateOrderRequest request, CancellationToken ct)
        => (await orderService.UpdateAsync(id, request, CurrentEmployeeId(), ct)).ToOk();

    /// <summary>取消訂單（軟刪除 = is_canceled，還原庫存）。</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await orderService.CancelAsync(id, CurrentEmployeeId(), ct);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>顯式取消同義端點。</summary>
    [HttpPost("{id:int}/cancel")]
    public Task<IActionResult> CancelExplicit(int id, CancellationToken ct) => Cancel(id, ct);

    private int CurrentEmployeeId()
        => int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : 0;
}
