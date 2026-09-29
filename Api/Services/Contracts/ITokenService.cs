using Api.Models;

namespace Api.Services.Contracts;

public interface ITokenService
{
    string CreateToken(ApplicationUser user, IReadOnlyList<string> roles);
}
