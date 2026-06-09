using Core.Configuration;
using Core.Services;
using Core.Users;
using Xunit;

namespace Tests;

public sealed class TokenServiceTests
{
    private readonly JwtSettings _jwtSettings = new()
    {
        Issuer = "cleanmap-api",
        Audience = "cleanmap-users",
        SecretKey = "test-super-secret-key-at-least-32-characters",
        AccessTokenExpiryMinutes = 60
    };

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var service = new TokenService(_jwtSettings);
        var user = new User { Id = "user1", Email = "test@example.com", Username = "testuser", Role = "admin" };

        var token = service.GenerateAccessToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        // Simple heuristic for JWT (3 parts separated by dots)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsRandomString()
    {
        var service = new TokenService(_jwtSettings);
        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void HashToken_ReturnsConsistentHash()
    {
        var service = new TokenService(_jwtSettings);
        var token = "some-random-refresh-token";

        var hash1 = service.HashToken(token);
        var hash2 = service.HashToken(token);

        Assert.False(string.IsNullOrWhiteSpace(hash1));
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(token, hash1); // Ensure it's actually hashed
    }
}
