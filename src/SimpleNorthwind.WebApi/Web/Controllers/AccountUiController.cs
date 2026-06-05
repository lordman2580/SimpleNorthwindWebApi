using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>登入 / 登出 / 拒絕存取（UI，Cookie scheme）。JWT 生命週期見 19-前端架構與整合 §5。</summary>
[Route("account")]
public sealed class AccountUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    private const string LoginView = "~/Web/Views/Account/Login.cshtml";

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl, [FromQuery] int expired = 0)
    {
        var model = new LoginViewModel { ReturnUrl = returnUrl };
        if (expired == 1)
            model.ErrorMessage = "登入已過期，請重新登入。";
        return View(LoginView, model);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ValidateModeFields(model))
            return View(LoginView, model);

        var result = model.LoginMode == "id"
            ? await apiClient.LoginAsync(new LoginRequest(model.EmployeeId!.Value, model.Password), cancellationToken)
            : await apiClient.LoginByNameAsync(new LoginByNameRequest(model.FirstName!, model.LastName!, model.Password), cancellationToken);

        if (!result.IsSuccess)
        {
            model.Password = string.Empty;
            // 不洩漏帳號列舉細節：登入失敗一律統一訊息（API 401 對所有失敗回相同 ProblemDetails）。
            model.ErrorMessage = "登入失敗：帳號或密碼錯誤。";
            return View(LoginView, model);
        }

        await SignInWithJwtAsync(result.Value!, model.RememberMe).ConfigureAwait(false);
        return LocalRedirect(string.IsNullOrEmpty(model.ReturnUrl) ? "/" : model.ReturnUrl);
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieScheme).ConfigureAwait(false);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("denied")]
    [AllowAnonymous]
    public IActionResult Denied()
        => View(LoginView, new LoginViewModel { ErrorMessage = "沒有存取權限，請以有權限的帳號登入。" });

    /// <summary>依登入模式條件驗證必填欄位；不符時填入 ModelState 並回 <c>false</c>。</summary>
    private bool ValidateModeFields(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return false;

        if (model.LoginMode == "id")
        {
            if (model.EmployeeId is null or <= 0)
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "請輸入有效的員工編號。");
                return false;
            }
        }
        else if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
        {
            ModelState.AddModelError(nameof(model.FirstName), "請輸入名與姓。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 解析 JWT 取 claims 與到期（<c>exp</c>，UTC 權威值），建立 Cookie principal
    /// （含 <c>jwt</c> claim 供 <see cref="BearerTimeZoneHandler"/> 帶 Bearer）。Cookie 到期對齊 JWT。
    /// </summary>
    private async Task SignInWithJwtAsync(string token, bool isPersistent)
    {
        var jwt = new JsonWebToken(token);
        var name = jwt.TryGetClaim("name", out var nameClaim) ? nameClaim.Value : string.Empty;
        var title = jwt.TryGetClaim("title", out var titleClaim) ? titleClaim.Value : string.Empty;

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, jwt.Subject),
                new Claim(ClaimTypes.Name, name),
                new Claim("title", title),
                new Claim("jwt", token)
            ],
            CookieScheme);

        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(jwt.ValidTo, DateTimeKind.Utc))
        };

        await HttpContext.SignInAsync(CookieScheme, new ClaimsPrincipal(identity), properties).ConfigureAwait(false);
    }
}
