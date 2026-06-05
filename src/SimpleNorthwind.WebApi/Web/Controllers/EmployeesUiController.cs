using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Employees;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// 員工名冊（唯讀）UI controller。透過 loopback typed client 取得員工清單，
/// 以卡片格呈現；不提供任何寫入操作。
/// 認證 / 稽核略過由 <see cref="UiControllerBase"/> 繼承提供。
/// </summary>
[Route("employees")]
public sealed class EmployeesUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    /// <summary>員工名冊頁：載入全部員工並映射為唯讀檢視模型。</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await apiClient.ListEmployeesAsync(ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect)
            return redirect;

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Detail ?? "載入員工資料失敗。";
            return View("~/Web/Views/Employees/Index.cshtml", Array.Empty<EmployeeViewModel>());
        }

        var vm = MapToViewModels(result.Value!);
        return View("~/Web/Views/Employees/Index.cshtml", vm);
    }

    /// <summary>將 API DTO 映射為唯讀檢視模型清單。</summary>
    private static IReadOnlyList<EmployeeViewModel> MapToViewModels(IReadOnlyList<EmployeeDto> employees)
        => [.. employees.Select(e => new EmployeeViewModel
        {
            EmployeeId = e.EmployeeId,
            FullName = e.FullName,
            Title = e.Title,
            PhoneNumber = e.PhoneNumber,
            PhoneExtNumber = e.PhoneExtNumber,
            HireDate = e.HireDate,
            IsResigned = e.IsResigned,
        })];
}
