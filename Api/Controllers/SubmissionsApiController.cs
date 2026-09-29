using System.Security.Claims;
using Api.Models;
using Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize(Roles = "Preparer,Reviewer,Admin")]
public sealed class SubmissionsController(ISubmissionService submissions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await submissions.ListAsync(Actor(), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Preparer")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var submission = await submissions.CreateAsync(Actor(), cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = submission.Id }, new { id = submission.Id, status = submission.Status });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await submissions.GetAsync(id, Actor(), cancellationToken);

        return result.Succeeded ? Ok(result.Value) : NotFound();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Preparer")]
    public async Task<IActionResult> SaveDraft(Guid id, [FromBody] SaveSubmissionRequest request, CancellationToken cancellationToken)
    {
        var result = await submissions.SaveDraftAsync(id, request.Form, Actor(), cancellationToken);
        if (result.Succeeded) return Ok(result.Value);

        if (result.StatusCode == 404) return NotFound();

        if (result.StatusCode == 409) return Conflict(new { error = result.Error });

        return BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<IActionResult> Validate(Guid id, CancellationToken cancellationToken)
    {
        var result = await submissions.ValidateAsync(id, Actor(), cancellationToken);
        if (!result.Succeeded)
        {
            if (result.StatusCode == 404) return NotFound();

            if (result.StatusCode == 409) return Conflict(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Preparer")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var result = await submissions.SubmitAsync(id, Actor(), cancellationToken);
        if (result.Succeeded) return Ok(result.Value);

        if (result.StatusCode == 404) return NotFound();

        if (result.StatusCode == 409) return Conflict(new { error = result.Error });

        if (result.StatusCode == 422) return UnprocessableEntity(new { findings = result.Findings ?? [] });

        return BadRequest(new { error = result.Error });
    }

    private SubmissionActor Actor() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        User.IsInRole("Preparer"),
        User.IsInRole("Reviewer"),
        User.IsInRole("Admin"));
}
