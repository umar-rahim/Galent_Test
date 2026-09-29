using Api.Models;
using Api.Repositories;
using Api.Services.Contracts;

namespace Api.Services;

public sealed class DocumentService(
    ISubmissionRepository submissions,
    IDocumentRepository documents,
    IDocumentStore documentStore,
    IForm1040Calculator calculator,
    IForm1040Validator validator,
    IForm1040PdfService pdfService,
    ILogger<DocumentService> logger) : IDocumentService
{
    public async Task<DocumentGenerationResult> GenerateAsync(Guid submissionId, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = await submissions.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null || !CanAccess(submission.UserId, actor))
            return new(false, null, null, null, 404);
        if (submission.Status != SubmissionStatus.Submitted)
        {
            logger.LogWarning("PDF generation rejected for submission {SubmissionId} because it is not submitted.", submissionId);
            return new(false, null, null, "Only submitted returns can generate a PDF.", 409);
        }

        calculator.Recalculate(submission.Form);
        var findings = validator.Validate(submission.Form);
        if (findings.Any(item => item.Severity == FindingSeverity.Error))
        {
            var records = findings.Select(item => new ValidationFinding
            {
                SubmissionId = submission.Id, Code = item.Code, Severity = item.Severity, Field = item.Field, Message = item.Message
            }).ToArray();
            await submissions.ReplaceFindingsAsync(submission, records, cancellationToken);
            logger.LogWarning("PDF generation validation failed for submission {SubmissionId} with {FindingCount} findings.", submissionId, findings.Count);
            return new(false, null,
                findings.Select(item => new ValidationFindingResponse(item.Code, item.Severity, item.Field, item.Message)).ToArray(),
                "Validation failed.", 422);
        }

        await submissions.ReplaceFindingsAsync(submission, [], cancellationToken);
        await using var generated = await pdfService.GeneratePdfAsync(submission);
        var stored = await documentStore.SaveAsync(submission.UserId, submission.Id, generated, cancellationToken);
        var document = new GeneratedDocument { SubmissionId = submission.Id, RelativePath = stored.RelativePath, Sha256 = stored.Sha256 };
        await documents.AddAsync(document, cancellationToken);
        var download = await documentStore.OpenReadAsync(stored.RelativePath, cancellationToken);
        logger.LogInformation("PDF generated and stored for submission {SubmissionId} as document {DocumentId}.", submissionId, document.Id);
        return new(true, new DocumentDownload(download, $"form-1040-2025-{submissionId:N}.pdf"), null, null, 200);
    }

    public async Task<IReadOnlyList<DocumentSummary>?> ListAsync(Guid submissionId, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = await submissions.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null || !CanAccess(submission.UserId, actor))
            return null;
        return await documents.ListAsync(submissionId, cancellationToken);
    }

    public async Task<DocumentDownload?> DownloadAsync(Guid submissionId, Guid documentId, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var document = await documents.GetForDownloadAsync(submissionId, documentId, cancellationToken);
        if (document is null || !CanAccess(document.Submission.UserId, actor))
            return null;

        try
        {
            var stream = await documentStore.OpenReadAsync(document.RelativePath, cancellationToken);
            return new DocumentDownload(stream, $"form-1040-{submissionId:N}.pdf");
        }
        catch (FileNotFoundException)
        {
            logger.LogWarning("Stored document file is missing for document {DocumentId}.", documentId);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogError("Access to stored document file was denied for document {DocumentId}.", documentId);
            return null;
        }
    }

    private static bool CanAccess(string ownerId, SubmissionActor actor) =>
        actor.IsAdmin || actor.IsReviewer || ownerId == actor.UserId;
}
