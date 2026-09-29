using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed class SaveSubmissionRequest
{
    public Form1040Data Form { get; set; } = new();
}

public sealed class CreateUserRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public sealed class ChangeUserRolesRequest
{
    [Required, MinLength(1)]
    public List<string> Roles { get; set; } = [];
}

public sealed record ValidationFindingResponse(string Code, FindingSeverity Severity, string Field, string Message);
