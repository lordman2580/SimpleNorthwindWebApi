using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.WebApi.Common;

namespace SimpleNorthwind.WebApi.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(CancellationToken ct)
        => Ok(await customerService.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Get(int id, CancellationToken ct)
        => (await customerService.GetAsync(id, ct)).ToOk();

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.CreateAsync(request, CurrentUser(), ct);
        return result.ToActionResult(value => CreatedAtAction(nameof(Get), new { id = value.CustomerId }, value));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, UpdateCustomerRequest request, CancellationToken ct)
        => (await customerService.UpdateAsync(id, request, ct)).ToOk();

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
