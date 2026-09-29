using Api.Models;
using Api.Repositories;
using Api.Services.Contracts;

namespace Api.Services;

public sealed class SubmissionService(
    ISubmissionRepository submissions,
    IForm1040Calculator calculator,
    IForm1040Validator validator) : ISubmissionService
{
    public async Task<IReadOnlyList<SubmissionListItem>> ListAsync(SubmissionActor actor, CancellationToken cancellationToken)
    {
        var ownerId = actor.IsPreparer && !actor.IsReviewer && !actor.IsAdmin ? actor.UserId : null;
        return (await submissions.ListAsync(ownerId, cancellationToken))
            .Select(item => new SubmissionListItem(item.Id, item.Status, item.CreatedAtUtc, item.UpdatedAtUtc,
                item.SubmittedAtUtc, item.TaxpayerName, MaskSsn(item.TaxpayerSsn))).ToArray();
    }

    // No-op overload to preserve legacy service contract after typed DTO refactor.
    public Task<IReadOnlyList<SubmissionListItem>> ListAsync(SubmissionActor actor) =>
        ListAsync(actor, CancellationToken.None);

    public async Task<Form1040Submission> CreateAsync(SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = new Form1040Submission { UserId = actor.UserId, Form = new Form1040Data() };
        await submissions.AddAsync(submission, cancellationToken);
        return submission;
    }

    public async Task<UseCaseResult<object>> GetAsync(Guid id, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = await submissions.GetByIdAsync(id, cancellationToken);
        if (submission is null || !CanAccess(submission, actor))
            return new(false, null, null, 404);

        var isOwnerPreparer = actor.IsPreparer && submission.UserId == actor.UserId;
        return new(true, ToResponse(submission, isOwnerPreparer));
    }

    public async Task<UseCaseResult<object>> SaveDraftAsync(Guid id, Form1040Data form, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = await submissions.GetByIdAsync(id, cancellationToken);
        if (submission is null || submission.UserId != actor.UserId || !actor.IsPreparer)
            return new(false, null, null, 404);
        if (submission.Status != SubmissionStatus.Draft)
            return new(false, null, "Only draft submissions can be edited.", 409);
        if (form.Dependents is null)
            return new(false, null, "Form data and dependent list are required.", 400);
        if (form.Dependents.Count > 4)
            return new(false, null, "At most four dependents may be listed. Set the additional-dependents indicator for others.", 400);

        form.Id = submission.Form.Id;
        form.SubmissionId = submission.Id;
        calculator.Recalculate(form);
        await submissions.UpdateDraftAsync(submission, form, cancellationToken);
        return new(true, ToResponse(submission, true));
    }

    public async Task<UseCaseResult<ValidationResultDto>> ValidateAsync(Guid id, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = await submissions.GetByIdAsync(id, cancellationToken);
        if (submission is null || !CanAccess(submission, actor))
            return new(false, null, null, 404);
        if (submission.Status != SubmissionStatus.Draft && !actor.IsReviewer && !actor.IsAdmin)
            return new(false, null, "Only draft submissions can be validated unless you have reviewer access.", 409);

        calculator.Recalculate(submission.Form);
        var findings = validator.Validate(submission.Form);
        var records = findings.Select(finding => new ValidationFinding
        {
            SubmissionId = submission.Id,
            Code = finding.Code,
            Severity = finding.Severity,
            Field = finding.Field,
            Message = finding.Message
        }).ToList();
        await submissions.ReplaceFindingsAsync(submission, records, cancellationToken);

        var calculated = new CalculatedFormLines(submission.Form.Line1z, submission.Form.Line9, submission.Form.Line11a,
            submission.Form.Line15, submission.Form.Line24, submission.Form.Line33, submission.Form.Line34, submission.Form.Line37);
        var response = new ValidationResultDto(
            findings.All(finding => finding.Severity != FindingSeverity.Error),
            findings.Select(finding => new ValidationFindingResponse(finding.Code, finding.Severity, finding.Field, finding.Message)).ToArray(),
            calculated);
        return new(true, response);
    }

    public async Task<UseCaseResult<SubmissionStatusResult>> SubmitAsync(Guid id, SubmissionActor actor, CancellationToken cancellationToken)
    {
        var submission = await submissions.GetByIdAsync(id, cancellationToken);
        if (submission is null || !actor.IsPreparer || submission.UserId != actor.UserId)
            return new(false, null, null, 404);
        if (submission.Status != SubmissionStatus.Draft)
            return new(false, null, "Submission is not a draft.", 409);

        calculator.Recalculate(submission.Form);
        var findings = validator.Validate(submission.Form);
        if (findings.Any(finding => finding.Severity == FindingSeverity.Error))
        {
            var records = findings.Select(finding => new ValidationFinding
            {
                SubmissionId = submission.Id,
                Code = finding.Code,
                Severity = finding.Severity,
                Field = finding.Field,
                Message = finding.Message
            }).ToList();
            await submissions.ReplaceFindingsAsync(submission, records, cancellationToken);
            var responses = findings.Select(finding => new ValidationFindingResponse(finding.Code, finding.Severity, finding.Field, finding.Message)).ToArray();
            return new(false, null, "Validation failed.", 422, responses);
        }

        await submissions.SubmitAsync(submission, cancellationToken);
        return new(true, new SubmissionStatusResult(submission.Id, submission.Status, submission.SubmittedAtUtc));
    }

    private static bool CanAccess(Form1040Submission submission, SubmissionActor actor) =>
        actor.IsAdmin || actor.IsReviewer || submission.UserId == actor.UserId;

    private static object ToResponse(Form1040Submission submission, bool includeSensitive)
    {
        if (!includeSensitive)
        {
            submission.Form.TaxpayerSsn = MaskSsn(submission.Form.TaxpayerSsn);
            submission.Form.SpouseSsn = MaskSsn(submission.Form.SpouseSsn);
            submission.Form.RoutingNumber = MaskIdentifier(submission.Form.RoutingNumber);
            submission.Form.AccountNumber = MaskIdentifier(submission.Form.AccountNumber);
            submission.Form.TaxpayerIdentityProtectionPin = null;
            submission.Form.SpouseIdentityProtectionPin = null;
            submission.Form.DesigneePin = null;
            submission.Form.PreparerPtin = MaskIdentifier(submission.Form.PreparerPtin);
            submission.Form.PreparerFirmEin = MaskIdentifier(submission.Form.PreparerFirmEin);
            foreach (var dependent in submission.Form.Dependents)
                dependent.Ssn = MaskSsn(dependent.Ssn);
        }

        return new
        {
            submission.Id, submission.Status, submission.CreatedAtUtc, submission.UpdatedAtUtc, submission.SubmittedAtUtc,
            form = submission.Form,
            findings = submission.Findings.Select(finding => new ValidationFindingResponse(finding.Code, finding.Severity, finding.Field, finding.Message)),
            documents = submission.Documents.Select(document => new { document.Id, document.CreatedAtUtc })
        };
    }

    private static string MaskSsn(string? ssn) =>
        !string.IsNullOrWhiteSpace(ssn) && ssn.Length >= 4 ? $"***-**-{ssn[^4..]}" : "Not provided";

    private static string? MaskIdentifier(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : new string('*', Math.Max(0, value.Length - 4)) + value[^Math.Min(4, value.Length)..];
}
