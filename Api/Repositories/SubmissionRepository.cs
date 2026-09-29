using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public sealed class SubmissionRepository(ApplicationDbContext db) : ISubmissionRepository
{
    public async Task<IReadOnlyList<SubmissionSummary>> ListAsync(string? ownerUserId, CancellationToken cancellationToken)
    {
        var query = db.FormSubmissions.AsNoTracking().Include(item => item.Form).AsQueryable();
        if (ownerUserId is not null)
            query = query.Where(item => item.UserId == ownerUserId);

        return await query.OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new SubmissionSummary(
                item.Id,
                item.Status,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.SubmittedAtUtc,
                item.Form.TaxpayerFirstName + " " + item.Form.TaxpayerLastName,
                item.Form.TaxpayerSsn))
            .ToListAsync(cancellationToken);
    }

    public Task<Form1040Submission?> GetByIdAsync(Guid submissionId, CancellationToken cancellationToken) =>
        db.FormSubmissions
            .Include(item => item.Form).ThenInclude(form => form.Dependents)
            .Include(item => item.Findings)
            .Include(item => item.Documents)
            .SingleOrDefaultAsync(item => item.Id == submissionId, cancellationToken);

    public async Task AddAsync(Form1040Submission submission, CancellationToken cancellationToken)
    {
        db.FormSubmissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDraftAsync(Form1040Submission submission, Form1040Data incoming, CancellationToken cancellationToken)
    {
        incoming.Id = submission.Form.Id;
        incoming.SubmissionId = submission.Id;
        db.Entry(submission.Form).CurrentValues.SetValues(incoming);
        db.Dependents.RemoveRange(submission.Form.Dependents);
        submission.Form.Dependents.Clear();
        foreach (var dependent in incoming.Dependents)
        {
            dependent.Id = Guid.NewGuid();
            dependent.Form1040DataId = submission.Form.Id;
            submission.Form.Dependents.Add(dependent);
        }
        db.Dependents.AddRange(submission.Form.Dependents);
        db.ValidationFindings.RemoveRange(submission.Findings);
        submission.Findings.Clear();
        submission.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceFindingsAsync(Form1040Submission submission, IReadOnlyCollection<ValidationFinding> findings, CancellationToken cancellationToken)
    {
        db.ValidationFindings.RemoveRange(submission.Findings);
        submission.Findings = findings.ToList();
        db.ValidationFindings.AddRange(submission.Findings);
        submission.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearFindingsAsync(Form1040Submission submission, CancellationToken cancellationToken)
    {
        db.ValidationFindings.RemoveRange(submission.Findings);
        submission.Findings.Clear();
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}