using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrativeEngine.Api.Requests;
using NarrativeEngine.Api.Services.Interfaces;

namespace NarrativeEngine.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshTokenCookie = "refresh_token";

    [HttpPost("register")]
    [EnableRateLimiting("auth-ip")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(new { accessToken = result.AccessToken });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-ip")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(new { accessToken = result.AccessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized();

        var result = await authService.RefreshTokenAsync(rawToken, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiry);
        return Ok(new { accessToken = result.AccessToken });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshTokenCookie];
        if (!string.IsNullOrEmpty(rawToken))
            await authService.RevokeAsync(rawToken, ct);

        Response.Cookies.Delete(RefreshTokenCookie);
        return NoContent();
    }

    private void SetRefreshTokenCookie(string rawToken, DateTime expiry)
    {
        Response.Cookies.Append(RefreshTokenCookie, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiry,
            Path = "/"
        });
    }
}
