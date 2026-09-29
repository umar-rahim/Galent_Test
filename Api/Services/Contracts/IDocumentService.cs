using Api.Models;
using Api.Repositories;

namespace Api.Services.Contracts;

public sealed record DocumentDownload(Stream Content, string FileName);
public sealed record DocumentGenerationResult(bool Succeeded, DocumentDownload? Download, IReadOnlyList<ValidationFindingResponse>? Findings, string? Error, int StatusCode);

public interface IDocumentService
{
    Task<DocumentGenerationResult> GenerateAsync(Guid submissionId, SubmissionActor actor, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentSummary>?> ListAsync(Guid submissionId, SubmissionActor actor, CancellationToken cancellationToken);

    Task<DocumentDownload?> DownloadAsync(Guid submissionId, Guid documentId, SubmissionActor actor, CancellationToken cancellationToken);
}
