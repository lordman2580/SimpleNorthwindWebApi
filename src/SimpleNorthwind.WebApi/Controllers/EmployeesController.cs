using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Employees;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>員工唯讀檢視。所有端點需 JWT。回應**絕不含密碼**。</summary>
[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    /// <summary>列出全部員工（姓名 / 職稱 / 分機 / 到職 / 在職狀態；無密碼）。</summary>
    /// <param name="ct">取消權杖。</param>
    /// <response code="200">回傳員工清單。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> List(CancellationToken ct)
        => Ok(await employeeService.ListAsync(ct));
}
