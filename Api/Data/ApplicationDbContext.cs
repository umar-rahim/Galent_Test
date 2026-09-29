using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Api.Models;

namespace Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Form1040Submission> FormSubmissions => Set<Form1040Submission>();
        public DbSet<Form1040Data> FormData => Set<Form1040Data>();
        public DbSet<Form1040Dependent> Dependents => Set<Form1040Dependent>();
        public DbSet<ValidationFinding> ValidationFindings => Set<ValidationFinding>();
        public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();
        public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Form1040Submission>()
                .HasOne(submission => submission.User)
                .WithMany(user => user.Submissions)
                .HasForeignKey(submission => submission.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Form1040Submission>()
                .HasOne(submission => submission.Form)
                .WithOne(form => form.Submission)
                .HasForeignKey<Form1040Data>(form => form.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Form1040Submission>()
                .HasMany(submission => submission.Findings)
                .WithOne(finding => finding.Submission)
                .HasForeignKey(finding => finding.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Form1040Submission>()
                .HasMany(submission => submission.Documents)
                .WithOne(document => document.Submission)
                .HasForeignKey(document => document.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Form1040Data>()
                .HasMany(form => form.Dependents)
                .WithOne()
                .HasForeignKey(dependent => dependent.Form1040DataId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RevokedToken>().HasIndex(token => token.ExpiresAtUtc);
        }
    }
}
