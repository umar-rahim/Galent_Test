using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Api.Models;
using Api.Services.Contracts;

namespace Api.Services
{
    public sealed class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(ApplicationUser user, IReadOnlyList<string> roles)
        {
            var secret = _config["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is required.");
            var key = Encoding.UTF8.GetBytes(secret);
            var issuer = _config["Jwt:Issuer"] ?? "Galent.Local";
            var audience = _config["Jwt:Audience"] ?? "Galent.Client";
            var lifetimeMinutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var configuredMinutes)
                ? Math.Clamp(configuredMinutes, 5, 60)
                : 30;

            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
