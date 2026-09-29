using Microsoft.AspNetCore.Identity;

namespace Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Form1040Submission> Submissions { get; set; } = new List<Form1040Submission>();
    }
}
