namespace Api.Repositories;

public interface IRevokedTokenRepository
{
    Task<bool> IsRevokedAsync(string jwtId, CancellationToken cancellationToken);
    Task RevokeAsync(string jwtId, DateTime expiresAtUtc, CancellationToken cancellationToken);
}