using Api.Models;

namespace Api.Repositories;

public sealed record SubmissionSummary(
    Guid Id,
    SubmissionStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? SubmittedAtUtc,
    string TaxpayerName,
    string? TaxpayerSsn);

public interface ISubmissionRepository
{
    Task<IReadOnlyList<SubmissionSummary>> ListAsync(string? ownerUserId, CancellationToken cancellationToken);
    Task<Form1040Submission?> GetByIdAsync(Guid submissionId, CancellationToken cancellationToken);
    Task AddAsync(Form1040Submission submission, CancellationToken cancellationToken);
    Task UpdateDraftAsync(Form1040Submission submission, Form1040Data incoming, CancellationToken cancellationToken);
    Task ReplaceFindingsAsync(Form1040Submission submission, IReadOnlyCollection<ValidationFinding> findings, CancellationToken cancellationToken);
    Task SubmitAsync(Form1040Submission submission, CancellationToken cancellationToken);
    Task ClearFindingsAsync(Form1040Submission submission, CancellationToken cancellationToken);
}