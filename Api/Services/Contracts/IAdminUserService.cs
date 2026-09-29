using Api.Models;

namespace Api.Services.Contracts;

public sealed record AdminUserSummary(string Id, string? Email, IReadOnlyList<string> Roles, DateTimeOffset? LockoutEnd);
public sealed record AdminUserCreated(string Id, string? Email, string Role);
public sealed record ServiceResult<T>(bool Succeeded, T? Value, string? Error, int StatusCode = 400, IReadOnlyList<string>? Errors = null);

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserSummary>> ListAsync(CancellationToken cancellationToken);

    Task<ServiceResult<AdminUserCreated>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<string>>> ChangeRolesAsync(string userId, ChangeUserRolesRequest request, CancellationToken cancellationToken);
}
