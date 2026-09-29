using System.IdentityModel.Tokens.Jwt;
using Api.Models;
using Api.Repositories;
using Api.Services.Contracts;

namespace Api.Services;

public sealed class AuthService(IUserRepository users, ITokenService tokens, IRevokedTokenRepository revokedTokens) : IAuthService
{
    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.AuthenticateAsync(request.Email.Trim(), request.Password, cancellationToken);
        if (user is null)
            return null;

        var tokenUser = new ApplicationUser { Id = user.Id, Email = user.Email, UserName = user.Email };
        var token = tokens.CreateToken(tokenUser, user.Roles);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return new LoginResult(token, parsed.ValidTo, new AuthenticatedUser(user.Id, user.Email, user.Roles));
    }

    public async Task<bool> LogoutAsync(string? jwtId, string? expiresClaim, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jwtId) || !long.TryParse(expiresClaim, out var seconds))
            return false;

        await revokedTokens.RevokeAsync(jwtId, DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime, cancellationToken);
        return true;
    }

    public Task<bool> IsRevokedAsync(string jwtId, CancellationToken cancellationToken) =>
        revokedTokens.IsRevokedAsync(jwtId, cancellationToken);
}
