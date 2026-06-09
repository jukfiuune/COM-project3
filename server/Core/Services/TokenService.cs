using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Configuration;
using Core.Users;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
namespace Core.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
