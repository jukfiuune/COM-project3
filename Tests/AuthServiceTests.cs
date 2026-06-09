using Core.Configuration;
using Core.DTOs;
using Core.Services;
using Core.Users;
using Xunit;

namespace Tests;

public sealed class AuthServiceTests
{
    private readonly JwtSettings _jwtSettings = new()
    {
        Issuer = "cleanmap-api",
        Audience = "cleanmap-users",
        SecretKey = "test-super-secret-key-at-least-32-characters",
        AccessTokenExpiryMinutes = 60
    };

    private readonly IPasswordService _passwordService = new PasswordService();
    private readonly ITokenService _tokenService;

    public AuthServiceTests() => _tokenService = new TokenService(_jwtSettings);

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenEmailIsInvalid()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "not-an-email",
            Password = "Password1!",
            Username = "user123"
        });

        Assert.Null(response);
        Assert.Equal("Invalid email format.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenPasswordIsWeak()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "weak",
            Username = "user123"
        });

        Assert.Null(response);
        Assert.Equal("Password must be at least 8 characters.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenUsernameTooShort()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "Password1!",
            Username = "us"
        });

        Assert.Null(response);
        Assert.Equal("Username must be at least 3 characters.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenUsernameTooLong()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "Password1!",
            Username = new string('a', 31)
        });

        Assert.Null(response);
        Assert.Equal("Username must be at most 30 characters.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenEmailAlreadyExists()
    {
        var repo = new FakeUserRepository();
        await repo.CreateAsync(new User { Email = "user@example.com", Username = "olduser" });
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "Password1!",
            Username = "newuser"
        });

        Assert.Null(response);
        Assert.Equal("An account with this email already exists.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenUsernameAlreadyExists()
    {
        var repo = new FakeUserRepository();
        await repo.CreateAsync(new User { Email = "old@example.com", Username = "user123" });
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "new@example.com",
            Password = "Password1!",
            Username = "user123"
        });

        Assert.Null(response);
        Assert.Equal("This username is already taken.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsAuthResponse_WhenValid()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "Password1!",
            Username = "user123"
        });

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.NotNull(response!.AccessToken);
        Assert.NotNull(response.RefreshToken);
        Assert.Equal("user123", response.User.Username);
        Assert.Equal("user@example.com", response.User.Email);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenUserDoesNotExist()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.LoginAsync(new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password1!"
        });

        Assert.Null(response);
        Assert.Equal("Invalid email or password.", error);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenPasswordIsInvalid()
    {
        var repo = new FakeUserRepository();
        var createdUser = new User
        {
            Id = "user-1",
            Username = "user123",
            Email = "user@example.com",
            PasswordHash = _passwordService.Hash("Password1!"),
            Role = "citizen"
        };

        await repo.CreateAsync(createdUser);
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong-password"
        });

        Assert.Null(response);
        Assert.Equal("Invalid email or password.", error);
    }

    [Fact]
    public async Task LoginAsync_ReturnsAuthResponse_WhenValid()
    {
        var repo = new FakeUserRepository();
        var createdUser = new User
        {
            Id = "user-2",
            Username = "user123",
            Email = "user@example.com",
            PasswordHash = _passwordService.Hash("Password1!"),
            Role = "citizen"
        };

        await repo.CreateAsync(createdUser);
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password1!"
        });

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.NotNull(response!.AccessToken);
        Assert.NotNull(response.RefreshToken);
        Assert.Equal("user123", response.User.Username);
        Assert.Equal("user@example.com", response.User.Email);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsError_WhenTokenInvalid()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.RefreshTokenAsync("invalid-token");

        Assert.Null(response);
        Assert.Equal("Invalid or expired refresh token.", error);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsError_WhenUserNotFound()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var token = "valid-token";
        await tokenRepo.CreateAsync(new Core.Entities.RefreshToken
        {
            UserId = "nonexistent-user",
            TokenHash = _tokenService.HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        });

        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.RefreshTokenAsync(token);

        Assert.Null(response);
        Assert.Equal("User not found.", error);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsAuthResponse_WhenValid()
    {
        var repo = new FakeUserRepository();
        var user = await repo.CreateAsync(new User { Username = "user123", Email = "test@example.com" });
        var tokenRepo = new FakeRefreshTokenRepository();
        var token = "valid-token";
        var entity = new Core.Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        await tokenRepo.CreateAsync(entity);

        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var (response, error) = await service.RefreshTokenAsync(token);

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.True(entity.IsRevoked); // Old token should be revoked
        Assert.NotEqual(token, response!.RefreshToken); // Should be a new token
    }

    [Fact]
    public async Task RevokeTokenAsync_ReturnsFalse_WhenTokenInvalid()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var result = await service.RevokeTokenAsync("invalid-token");

        Assert.False(result);
    }

    [Fact]
    public async Task RevokeTokenAsync_ReturnsTrue_WhenValid()
    {
        var repo = new FakeUserRepository();
        var tokenRepo = new FakeRefreshTokenRepository();
        var token = "valid-token";
        var entity = new Core.Entities.RefreshToken
        {
            UserId = "user1",
            TokenHash = _tokenService.HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        await tokenRepo.CreateAsync(entity);

        var service = new AuthService(repo, _passwordService, _tokenService, tokenRepo);

        var result = await service.RevokeTokenAsync(token);

        Assert.True(result);
        Assert.True(entity.IsRevoked);
    }
}
