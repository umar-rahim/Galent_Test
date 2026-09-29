using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed class GeneratedDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Form1040Submission Submission { get; set; } = null!;
    [Required, StringLength(500)] public string RelativePath { get; set; } = string.Empty;
    [Required, StringLength(64)] public string Sha256 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
