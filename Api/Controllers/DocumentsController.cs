using Api.Models;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/submissions/{submissionId:guid}/documents")]
[Authorize(Roles = "Preparer,Reviewer,Admin")]
public sealed class DocumentsController : ControllerBase
{
    private readonly ISubmissionRepository _submissions;
    private readonly IDocumentRepository _documents;
    private readonly IDocumentStore _documentStore;
    private readonly IForm1040Calculator _calculator;
    private readonly IForm1040Validator _validator;
    private readonly IForm1040PdfService _pdfService;

    public DocumentsController(
        ISubmissionRepository submissions,
        IDocumentRepository documents,
        IDocumentStore documentStore,
        IForm1040Calculator calculator,
        IForm1040Validator validator,
        IForm1040PdfService pdfService)
    {
        _submissions = submissions;
        _documents = documents;
        _documentStore = documentStore;
        _calculator = calculator;
        _validator = validator;
        _pdfService = pdfService;
    }

    [HttpPost]
    public async Task<IActionResult> Generate(Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null || !CanAccess(submission.UserId))
            return NotFound();

        _calculator.Recalculate(submission.Form);
        var findings = _validator.Validate(submission.Form);
        var findingRecords = findings.Select(finding => new ValidationFinding
        {
            SubmissionId = submission.Id,
            Code = finding.Code,
            Severity = finding.Severity,
            Field = finding.Field,
            Message = finding.Message
        }).ToList();

        if (findings.Any(finding => finding.Severity == FindingSeverity.Error))
        {
            await _submissions.ReplaceFindingsAsync(submission, findingRecords, cancellationToken);
            return UnprocessableEntity(new
            {
                findings = findings.Select(finding => new ValidationFindingResponse(finding.Code, finding.Severity, finding.Field, finding.Message))
            });
        }

        await _submissions.ReplaceFindingsAsync(submission, findingRecords, cancellationToken);

        await using var generatedPdf = await _pdfService.GeneratePdfAsync(submission);
        var stored = await _documentStore.SaveAsync(submission.UserId, submission.Id, generatedPdf, cancellationToken);
        var document = new GeneratedDocument
        {
            SubmissionId = submission.Id,
            RelativePath = stored.RelativePath,
            Sha256 = stored.Sha256
        };
        await _documents.AddAsync(document, cancellationToken);

        var download = await _documentStore.OpenReadAsync(stored.RelativePath, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(download, "application/pdf", $"form-1040-2025-{submissionId:N}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null || !CanAccess(submission.UserId))
            return NotFound();

        Response.Headers.CacheControl = "no-store";
        var documents = await _documents.ListAsync(submissionId, cancellationToken);
        return Ok(documents);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Download(Guid submissionId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documents.GetForDownloadAsync(submissionId, documentId, cancellationToken);
        if (document is null || !CanAccess(document.Submission.UserId))
            return NotFound();

        try
        {
            var stream = await _documentStore.OpenReadAsync(document.RelativePath, cancellationToken);
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(stream, "application/pdf", $"form-1040-{submissionId:N}.pdf");
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    private bool CanAccess(string ownerId) =>
        User.IsInRole("Admin") || User.IsInRole("Reviewer") || ownerId == User.FindFirstValue(ClaimTypes.NameIdentifier);
}
