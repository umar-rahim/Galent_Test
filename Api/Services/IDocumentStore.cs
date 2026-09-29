namespace Api.Services;

public sealed record StoredDocument(string RelativePath, string Sha256);

public interface IDocumentStore
{
    Task<StoredDocument> SaveAsync(string userId, Guid submissionId, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken);
}
