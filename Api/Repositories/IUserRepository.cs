using Api.Models;

namespace Api.Repositories;

public interface IUserRepository
{
    Task<AuthenticatedUserRecord?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminUserSummaryRecord>> ListAsync(CancellationToken cancellationToken);
    Task<UserCreateResult> CreateAsync(string email, string password, string role, CancellationToken cancellationToken);
    Task<RoleChangeResult> ChangeRolesAsync(string userId, IReadOnlyList<string> requested, CancellationToken cancellationToken);
}

public sealed record AdminUserSummaryRecord(string Id, string? Email, IReadOnlyList<string> Roles, DateTimeOffset? LockoutEnd);
public sealed record UserCreateResult(bool Succeeded, string? Id, string? Email, IReadOnlyList<string> Errors);
public sealed record AuthenticatedUserRecord(string Id, string? Email, IReadOnlyList<string> Roles);
public sealed record RoleChangeResult(bool Succeeded, bool NotFound, bool LastAdminConflict, IReadOnlyList<string> Roles, IReadOnlyList<string> Errors);
