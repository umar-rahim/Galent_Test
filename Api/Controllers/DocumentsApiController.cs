using System.Security.Claims;
using Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/submissions/{submissionId:guid}/documents")]
[Authorize(Roles = "Preparer,Reviewer,Admin")]
public sealed class DocumentsController(IDocumentService documents) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Generate(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await documents.GenerateAsync(submissionId, Actor(), cancellationToken);

        if (!result.Succeeded)
        {
            if (result.StatusCode == 404) return NotFound();

            if (result.StatusCode == 409) return Conflict(new { error = result.Error });

            if (result.StatusCode == 422) return UnprocessableEntity(new { findings = result.Findings });

            return StatusCode(result.StatusCode, new { error = result.Error });
        }

        Response.Headers.CacheControl = "no-store";

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(result.Download!.Content, "application/pdf", result.Download.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await documents.ListAsync(submissionId, Actor(), cancellationToken);

        if (result is null) return NotFound();

        Response.Headers.CacheControl = "no-store";

        return Ok(result);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Download(Guid submissionId, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await documents.DownloadAsync(submissionId, documentId, Actor(), cancellationToken);

        if (result is null) return NotFound();

        Response.Headers.CacheControl = "no-store";

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(result.Content, "application/pdf", result.FileName);
    }

    private SubmissionActor Actor() => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,

        User.IsInRole("Preparer"), User.IsInRole("Reviewer"), User.IsInRole("Admin"));
}
