using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Models;
using Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new { error = "Invalid credentials." });

        return Ok(new
        {
            accessToken = result.AccessToken,
            expiresAtUtc = result.ExpiresAtUtc,
            user = new { id = result.User.Id, email = result.User.Email, roles = result.User.Roles }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var valid = await authService.LogoutAsync(
            User.FindFirstValue(JwtRegisteredClaimNames.Jti),
            User.FindFirstValue(JwtRegisteredClaimNames.Exp),
            cancellationToken);
        return valid ? NoContent() : Unauthorized();
    }
}
