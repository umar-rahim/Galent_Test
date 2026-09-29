using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public sealed class RevokedToken
{
    [Key]
    public string JwtId { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}