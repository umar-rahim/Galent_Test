using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public sealed class DocumentRepository(ApplicationDbContext db) : IDocumentRepository
{
    public async Task<IReadOnlyList<DocumentSummary>> ListAsync(Guid submissionId, CancellationToken cancellationToken) =>
        await db.GeneratedDocuments.AsNoTracking()
            .Where(document => document.SubmissionId == submissionId)
            .OrderByDescending(document => document.CreatedAtUtc)
            .Select(document => new DocumentSummary(document.Id, document.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public Task<GeneratedDocument?> GetForDownloadAsync(Guid submissionId, Guid documentId, CancellationToken cancellationToken) =>
        db.GeneratedDocuments.AsNoTracking()
            .Include(document => document.Submission)
            .SingleOrDefaultAsync(document => document.Id == documentId && document.SubmissionId == submissionId, cancellationToken);

    public async Task AddAsync(GeneratedDocument document, CancellationToken cancellationToken)
    {
        db.GeneratedDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
    }
}