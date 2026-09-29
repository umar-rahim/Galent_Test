using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Admin", "Preparer", "Reviewer"];
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminUsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var results = new List<object>(users.Count);
        foreach (var user in users)
        {
            results.Add(new { user.Id, user.Email, roles = await _userManager.GetRolesAsync(user), user.LockoutEnd });
        }
        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!AllowedRoles.Contains(request.Role, StringComparer.Ordinal))
            return BadRequest(new { error = "Role must be Admin, Preparer, or Reviewer." });

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true
        };
        var createResult = await _userManager.CreateAsync(user, request.TemporaryPassword);
        if (!createResult.Succeeded)
            return BadRequest(new { errors = createResult.Errors.Select(error => new { error.Code, error.Description }) });

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Problem("The account could not be assigned its role.", statusCode: StatusCodes.Status500InternalServerError);
        }

        return Created("/api/admin/users", new { user.Id, user.Email, role = request.Role });
    }

    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> ChangeRoles(string userId, [FromBody] ChangeUserRolesRequest request)
    {
        var requestedRoles = request.Roles.Distinct(StringComparer.Ordinal).ToArray();
        if (requestedRoles.Length == 0 || requestedRoles.Any(role => !AllowedRoles.Contains(role, StringComparer.Ordinal)))
            return BadRequest(new { error = "Assign at least one valid role: Admin, Preparer, or Reviewer." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains("Admin", StringComparer.Ordinal)
            && !requestedRoles.Contains("Admin", StringComparer.Ordinal))
        {
            var otherAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            if (!otherAdmins.Any(admin => admin.Id != user.Id))
                return Conflict(new { error = "The last administrator cannot be demoted." });
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return BadRequest(new { errors = removeResult.Errors.Select(error => new { error.Code, error.Description }) });

        var addResult = await _userManager.AddToRolesAsync(user, requestedRoles);
        if (!addResult.Succeeded)
        {
            await _userManager.AddToRolesAsync(user, currentRoles);
            return BadRequest(new { errors = addResult.Errors.Select(error => new { error.Code, error.Description }) });
        }

        return Ok(new { user.Id, user.Email, roles = requestedRoles });
    }
}
