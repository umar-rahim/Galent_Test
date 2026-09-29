using Api.Models;

namespace Api.Repositories;

public sealed record DocumentSummary(Guid Id, DateTime CreatedAtUtc);

public interface IDocumentRepository
{
    Task<IReadOnlyList<DocumentSummary>> ListAsync(Guid submissionId, CancellationToken cancellationToken);
    Task<GeneratedDocument?> GetForDownloadAsync(Guid submissionId, Guid documentId, CancellationToken cancellationToken);
    Task AddAsync(GeneratedDocument document, CancellationToken cancellationToken);
}