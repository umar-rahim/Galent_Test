using Api.Models;
using Api.Repositories;
using Api.Services.Contracts;
using Microsoft.AspNetCore.Http;

namespace Api.Services;

public sealed class AdminUserService(IUserRepository users) : IAdminUserService
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
        var created = await users.CreateAsync(email, request.TemporaryPassword, cancellationToken);
        if (!created.Operation.Succeeded || created.User is null)
            return new(false, null, "Unable to create user.", Errors: created.Operation.Errors);

        var assigned = await users.AssignRoleAsync(created.User, request.Role, cancellationToken);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(created.User, cancellationToken);
            return new(false, null, "The account could not be assigned its role.", StatusCodes.Status500InternalServerError);
        }

        return new(true, new AdminUserCreated(created.User.Id, created.User.Email, request.Role), null, StatusCodes.Status201Created);
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
