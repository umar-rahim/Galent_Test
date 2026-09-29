using Api.Models;

namespace Api.Services.Contracts;

public sealed record AuthenticatedUser(string Id, string? Email, IReadOnlyList<string> Roles);
public sealed record LoginResult(string AccessToken, DateTime ExpiresAtUtc, AuthenticatedUser User);

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<bool> LogoutAsync(string? jwtId, string? expiresClaim, CancellationToken cancellationToken);
    Task<bool> IsRevokedAsync(string jwtId, CancellationToken cancellationToken);
}
