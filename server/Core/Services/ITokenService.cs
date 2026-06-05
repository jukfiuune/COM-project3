using System.Security.Claims;
using Core.Entities;

namespace Core.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
