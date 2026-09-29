using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public sealed class UserRepository(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IUserRepository
{
    public async Task<AuthenticatedUserRecord?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return null;
        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return null;
        return new AuthenticatedUserRecord(user.Id, user.Email, await userManager.GetRolesAsync(user));
    }

    public async Task<IReadOnlyList<AdminUserSummaryRecord>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var result = new List<AdminUserSummaryRecord>(users.Count);
        foreach (var user in users)
            result.Add(new AdminUserSummaryRecord(user.Id, user.Email, await userManager.GetRolesAsync(user), user.LockoutEnd));
        return result;
    }

    public async Task<UserCreateResult> CreateAsync(string email, string password, string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new UserCreateResult(false, null, null, result.Errors.Select(error => error.Code).ToArray());

        var assigned = await userManager.AddToRoleAsync(user, role);
        if (assigned.Succeeded)
            return new UserCreateResult(true, user.Id, user.Email, []);

        var rollback = await userManager.DeleteAsync(user);
        var errors = assigned.Errors.Select(error => error.Code).ToList();
        if (!rollback.Succeeded)
            errors.AddRange(rollback.Errors.Select(error => $"User cleanup failed: {error.Code}"));
        return new UserCreateResult(false, null, null, errors);
    }

    public async Task<RoleChangeResult> ChangeRolesAsync(string userId, IReadOnlyList<string> requested, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return new(false, true, false, [], []);

        var existing = await userManager.GetRolesAsync(user);
        if (existing.Contains("Admin", StringComparer.Ordinal)
            && !requested.Contains("Admin", StringComparer.Ordinal)
            && (await userManager.GetUsersInRoleAsync("Admin")).Count <= 1)
            return new(false, false, true, existing, []);

        var removed = await userManager.RemoveFromRolesAsync(user, existing);
        if (!removed.Succeeded)
            return new(false, false, false, existing, removed.Errors.Select(error => error.Code).ToArray());

        var added = await userManager.AddToRolesAsync(user, requested);
        if (added.Succeeded)
            return new(true, false, false, requested, []);

        var rollback = await userManager.AddToRolesAsync(user, existing);
        var errors = added.Errors.Select(error => error.Code).ToList();
        if (!rollback.Succeeded)
            errors.AddRange(rollback.Errors.Select(error => $"Role rollback failed: {error.Code}"));
        return new RoleChangeResult(false, false, false, existing, errors);
    }

}
