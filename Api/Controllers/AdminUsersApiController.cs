using Api.Models;
using Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController(IAdminUserService adminUsers) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await adminUsers.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await adminUsers.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.StatusCode == StatusCodes.Status500InternalServerError)
                return Problem(result.Error, statusCode: result.StatusCode);
            return BadRequest(new { errors = result.Errors ?? [result.Error ?? "Request failed."] });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> ChangeRoles(string userId, [FromBody] ChangeUserRolesRequest request, CancellationToken cancellationToken)
    {
        var result = await adminUsers.ChangeRolesAsync(userId, request, cancellationToken);
        if (result.Succeeded)
            return Ok(new { userId, roles = result.Value });
        if (result.StatusCode == StatusCodes.Status404NotFound)
            return NotFound();
        if (result.StatusCode == StatusCodes.Status409Conflict)
            return Conflict(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
