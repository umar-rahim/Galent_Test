using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public sealed class RevokedTokenRepository(ApplicationDbContext db) : IRevokedTokenRepository
{
    public Task<bool> IsRevokedAsync(string jwtId, CancellationToken cancellationToken) =>
        db.RevokedTokens.AnyAsync(token => token.JwtId == jwtId, cancellationToken);

    public async Task RevokeAsync(string jwtId, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        if (await IsRevokedAsync(jwtId, cancellationToken))
            return;

        db.RevokedTokens.Add(new RevokedToken { JwtId = jwtId, ExpiresAtUtc = expiresAtUtc });
        await db.SaveChangesAsync(cancellationToken);
    }
}