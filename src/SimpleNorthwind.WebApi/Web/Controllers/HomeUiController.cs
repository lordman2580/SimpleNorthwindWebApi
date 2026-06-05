using Microsoft.AspNetCore.Mvc;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// 首頁 / 導覽（UI，Cookie scheme 由 <see cref="UiControllerBase"/> 提供）。
/// 未登入存取 → Cookie handler 導向 /account/login。
/// F1：靜態歡迎頁；Dashboard 彙總資料於 F2 (S2) 經 loopback 接上。
/// </summary>
public sealed class HomeUiController : UiControllerBase
{
    [HttpGet("/")]
    public IActionResult Index() => View("~/Web/Views/Home/Index.cshtml");
}
