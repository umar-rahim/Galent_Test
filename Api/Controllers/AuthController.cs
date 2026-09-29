using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IRevokedTokenRepository _revokedTokens;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            IRevokedTokenRepository revokedTokens)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _revokedTokens = revokedTokens;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                return Unauthorized(new { error = "Invalid credentials." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return Unauthorized(new { error = "Invalid credentials." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.CreateToken(user, roles);
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return Ok(new
            {
                accessToken = token,
                expiresAtUtc = parsed.ValidTo,
                user = new { id = user.Id, email = user.Email, roles }
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (string.IsNullOrWhiteSpace(jti))
            {
                return Unauthorized();
            }

            var expiresClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
            if (!long.TryParse(expiresClaim, out var expirationSeconds))
            {
                return Unauthorized();
            }

            await _revokedTokens.RevokeAsync(jti, DateTimeOffset.FromUnixTimeSeconds(expirationSeconds).UtcDateTime, HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
