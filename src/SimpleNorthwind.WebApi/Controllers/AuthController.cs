using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.WebApi.Common;

namespace SimpleNorthwind.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>員工登入，成功回 JWT 與到期時間（client 本地時區）。</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.ToOk();
    }
}
