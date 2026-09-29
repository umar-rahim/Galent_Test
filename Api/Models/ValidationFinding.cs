using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed class ValidationFinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Form1040Submission Submission { get; set; } = null!;
    [Required, StringLength(80)] public string Code { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    [Required, StringLength(100)] public string Field { get; set; } = string.Empty;
    [Required, StringLength(300)] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum FindingSeverity
{
    Warning,
    Error
}
