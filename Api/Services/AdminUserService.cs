using Api.Models;
using Api.Repositories;
using Api.Services.Contracts;
using Microsoft.AspNetCore.Http;

namespace Api.Services;

public sealed class AdminUserService(IUserRepository users, ILogger<AdminUserService> logger) : IAdminUserService
{
    private static readonly string[] AllowedRoles = ["Admin", "Preparer", "Reviewer"];

    public async Task<IReadOnlyList<AdminUserSummary>> ListAsync(CancellationToken cancellationToken) =>
        (await users.ListAsync(cancellationToken))
        .Select(user => new AdminUserSummary(user.Id, user.Email, user.Roles, user.LockoutEnd))
        .ToArray();

    public async Task<ServiceResult<AdminUserCreated>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!AllowedRoles.Contains(request.Role, StringComparer.Ordinal))
            return new(false, null, "Role must be Admin, Preparer, or Reviewer.");

        var email = request.Email.Trim();
        var created = await users.CreateAsync(email, request.TemporaryPassword, request.Role, cancellationToken);
        if (!created.Succeeded || created.Id is null)
        {
            logger.LogWarning("Admin user creation failed. Identity error count: {ErrorCount}.", created.Errors.Count);
            return new(false, null, "Unable to create user.", Errors: created.Errors);
        }

        logger.LogInformation("Admin created user {UserId} with role {Role}.", created.Id, request.Role);
        return new(true, new AdminUserCreated(created.Id, created.Email, request.Role), null, StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> ChangeRolesAsync(string userId, ChangeUserRolesRequest request, CancellationToken cancellationToken)
    {
        var requested = request.Roles.Distinct(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0 || requested.Any(role => !AllowedRoles.Contains(role, StringComparer.Ordinal)))
            return new(false, null, "Assign at least one valid role: Admin, Preparer, or Reviewer.");

        var update = await users.ChangeRolesAsync(userId, requested, cancellationToken);
        if (update.NotFound)
            return new(false, null, null, StatusCodes.Status404NotFound);
        if (update.LastAdminConflict)
            return new(false, null, "The last administrator cannot be demoted.", StatusCodes.Status409Conflict);
        return update.Succeeded
            ? new(true, update.Roles, null, StatusCodes.Status200OK)
            : new(false, null, "Unable to change user roles.", Errors: update.Errors);
    }
}
