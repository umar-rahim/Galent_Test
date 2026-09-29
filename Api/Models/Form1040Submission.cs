using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public sealed class Form1040Submission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAtUtc { get; set; }

        public Form1040Data Form { get; set; } = new Form1040Data();
        public List<ValidationFinding> Findings { get; set; } = new List<ValidationFinding>();
        public List<GeneratedDocument> Documents { get; set; } = new List<GeneratedDocument>();
    }

    public enum SubmissionStatus
    {
        Draft,
        Submitted
    }
}
