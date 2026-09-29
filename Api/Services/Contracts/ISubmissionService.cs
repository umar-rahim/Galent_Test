using Api.Models;

namespace Api.Services.Contracts;

public sealed record SubmissionActor(string UserId, bool IsPreparer, bool IsReviewer, bool IsAdmin);
public sealed record UseCaseResult<T>(bool Succeeded, T? Value, string? Error = null, int StatusCode = 200, IReadOnlyList<ValidationFindingResponse>? Findings = null);
public sealed record CalculatedFormLines(decimal Line1z, decimal Line9, decimal Line11a, decimal Line15, decimal Line24, decimal Line33, decimal Line34, decimal Line37);
public sealed record ValidationResultDto(bool IsValid, IReadOnlyList<ValidationFindingResponse> Findings, CalculatedFormLines Calculated);
public sealed record SubmissionStatusResult(Guid Id, SubmissionStatus Status, DateTime? SubmittedAtUtc);

public interface ISubmissionService
{
    Task<IReadOnlyList<SubmissionListItem>> ListAsync(SubmissionActor actor, CancellationToken cancellationToken);
    Task<Form1040Submission> CreateAsync(SubmissionActor actor, CancellationToken cancellationToken);
    Task<UseCaseResult<object>> GetAsync(Guid id, SubmissionActor actor, CancellationToken cancellationToken);
    Task<UseCaseResult<object>> SaveDraftAsync(Guid id, Form1040Data form, SubmissionActor actor, CancellationToken cancellationToken);
    Task<UseCaseResult<ValidationResultDto>> ValidateAsync(Guid id, SubmissionActor actor, CancellationToken cancellationToken);
    Task<UseCaseResult<SubmissionStatusResult>> SubmitAsync(Guid id, SubmissionActor actor, CancellationToken cancellationToken);
}

public sealed record SubmissionListItem(Guid Id, SubmissionStatus Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? SubmittedAtUtc, string TaxpayerName, string MaskedSsn);
