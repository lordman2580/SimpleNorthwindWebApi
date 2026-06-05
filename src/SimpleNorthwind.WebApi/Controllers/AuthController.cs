using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.WebApi.Common;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>身分驗證：員工以員工編號 + 密碼登入並取得 JWT。</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>員工登入並取得 JWT。</summary>
    /// <remarks>
    /// 以員工編號與密碼（PBKDF2 雜湊）驗證。成功回傳 JWT 與到期時間；到期時間以呼叫端時區輸出
    /// （由 <c>X-Time-Zone</c> header 決定，未帶則退回預設 <c>Asia/Taipei</c>）。
    ///
    /// 為避免帳號列舉，「帳號不存在」「已離職」「密碼錯誤」皆回相同的 401。
    /// 後續呼叫受保護端點時，於 <c>Authorization</c> header 帶 <c>Bearer {token}</c>。
    /// </remarks>
    /// <param name="request">登入請求（員工編號 + 密碼）。</param>
    /// <param name="ct">取消權杖。</param>
    /// <returns>JWT 與到期時間。</returns>
    /// <response code="200">登入成功，回傳 token 與到期時間。</response>
    /// <response code="400">請求格式不符（如缺欄位、員工編號 ≤ 0）。</response>
    /// <response code="401">員工編號或密碼錯誤（含帳號不存在 / 已離職）。</response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.ToOk();
    }

    /// <summary>員工以姓名（first + last，<b>case-sensitive</b>）+ 密碼登入並取得 JWT。</summary>
    /// <remarks>
    /// 與 <c>POST /api/auth/login</c>（員工編號）並存。姓名比對為**區分大小寫**（序數比對）；
    /// 為避免帳號列舉，「查無此人」「大小寫不符」「已離職」「密碼錯誤」皆回相同的 401。
    /// </remarks>
    /// <response code="200">登入成功，回傳 token 與到期時間。</response>
    /// <response code="400">請求格式不符（姓名 / 密碼空白）。</response>
    /// <response code="401">姓名或密碼錯誤（含查無此人 / 大小寫不符 / 已離職）。</response>
    [HttpPost("login-by-name")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> LoginByName(LoginByNameRequest request, CancellationToken ct)
    {
        var result = await authService.LoginByNameAsync(request, ct);
        return result.ToOk();
    }
}
