using Api.Models;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize(Roles = "Preparer,Reviewer,Admin")]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISubmissionRepository _submissions;
    private readonly IForm1040Calculator _calculator;
    private readonly IForm1040Validator _validator;

    public SubmissionsController(ISubmissionRepository submissions, IForm1040Calculator calculator, IForm1040Validator validator)
    {
        _submissions = submissions;
        _calculator = calculator;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var ownerUserId = User.IsInRole("Preparer") ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var rows = (await _submissions.ListAsync(ownerUserId, cancellationToken))
            .Select(item => new
            {
                item.Id,
                item.Status,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.SubmittedAtUtc,
                item.TaxpayerName,
                MaskedSsn = MaskSsn(item.TaxpayerSsn)
            });

        Response.Headers.CacheControl = "no-store";
        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Roles = "Preparer")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var submission = new Form1040Submission
        {
            UserId = userId,
            Form = new Form1040Data()
        };
        await _submissions.AddAsync(submission, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return CreatedAtAction(nameof(Get), new { id = submission.Id }, new { id = submission.Id, status = submission.Status });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var submission = await LoadAsync(id, cancellationToken);
        if (submission is null || !CanAccess(submission))
            return NotFound();

        var includeSensitive = User.IsInRole("Preparer") && submission.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier);
        Response.Headers.CacheControl = "no-store";
        return Ok(ToResponse(submission, includeSensitive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Preparer")]
    public async Task<IActionResult> SaveDraft(Guid id, [FromBody] SaveSubmissionRequest request, CancellationToken cancellationToken)
    {
        var submission = await LoadAsync(id, cancellationToken);
        if (submission is null || submission.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return NotFound();
        if (submission.Status != SubmissionStatus.Draft)
            return Conflict(new { error = "Only draft submissions can be edited." });

        var incoming = request.Form;
        if (incoming is null || incoming.Dependents is null)
            return BadRequest(new { error = "Form data and dependent list are required." });
        if (incoming.Dependents.Count > 4)
            return BadRequest(new { error = "At most four dependents may be listed. Set the additional-dependents indicator for others." });

        incoming.Id = submission.Form.Id;
        incoming.SubmissionId = submission.Id;
        _calculator.Recalculate(incoming);
        await _submissions.UpdateDraftAsync(submission, incoming, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(ToResponse(submission, includeSensitive: true));
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<IActionResult> Validate(Guid id, CancellationToken cancellationToken)
    {
        var submission = await LoadAsync(id, cancellationToken);
        if (submission is null || !CanAccess(submission))
            return NotFound();

        if (submission.Status != SubmissionStatus.Draft && !User.IsInRole("Reviewer") && !User.IsInRole("Admin"))
            return Conflict(new { error = "Only draft submissions can be validated unless you have reviewer access." });

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
        await _submissions.ReplaceFindingsAsync(submission, findingRecords, cancellationToken);

        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            isValid = findings.All(finding => finding.Severity != FindingSeverity.Error),
            findings = findings.Select(finding => new ValidationFindingResponse(finding.Code, finding.Severity, finding.Field, finding.Message)),
            calculated = new
            {
                submission.Form.Line1z,
                submission.Form.Line9,
                submission.Form.Line11a,
                submission.Form.Line15,
                submission.Form.Line24,
                submission.Form.Line33,
                submission.Form.Line34,
                submission.Form.Line37
            }
        });
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Preparer")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var submission = await LoadAsync(id, cancellationToken);
        if (submission is null || submission.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return NotFound();
        if (submission.Status != SubmissionStatus.Draft)
            return Conflict(new { error = "Submission is not a draft." });

        _calculator.Recalculate(submission.Form);
        var findings = _validator.Validate(submission.Form);
        if (findings.Any(finding => finding.Severity == FindingSeverity.Error))
        {
            var findingRecords = findings.Select(finding => new ValidationFinding
            {
                SubmissionId = submission.Id,
                Code = finding.Code,
                Severity = finding.Severity,
                Field = finding.Field,
                Message = finding.Message
            }).ToList();
            await _submissions.ReplaceFindingsAsync(submission, findingRecords, cancellationToken);
            return UnprocessableEntity(new { findings = findings.Select(finding => new ValidationFindingResponse(finding.Code, finding.Severity, finding.Field, finding.Message)) });
        }

        submission.Status = SubmissionStatus.Submitted;
        submission.SubmittedAtUtc = DateTime.UtcNow;
        submission.UpdatedAtUtc = DateTime.UtcNow;
        await _submissions.ClearFindingsAsync(submission, cancellationToken);
        return Ok(new { id = submission.Id, status = submission.Status, submittedAtUtc = submission.SubmittedAtUtc });
    }

    private Task<Form1040Submission?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        _submissions.GetByIdAsync(id, cancellationToken);

    private bool CanAccess(Form1040Submission submission) =>
        User.IsInRole("Admin") || User.IsInRole("Reviewer") || submission.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            submission.Id,
            submission.Status,
            submission.CreatedAtUtc,
            submission.UpdatedAtUtc,
            submission.SubmittedAtUtc,
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
