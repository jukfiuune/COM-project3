using System.Text.RegularExpressions;
using Core.Configuration;
using Core.DTOs;
using Core.Repositories;
using Core.Users;

namespace Core.Services;

public sealed partial class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public AuthService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<(AuthResponse? Response, string? Error)> SignupAsync(SignupRequest request)
    {
        if (!EmailRegex().IsMatch(request.Email))
            return (null, "Invalid email format.");

        var (isValid, error) = _passwordService.ValidateComplexity(request.Password);
        if (!isValid)
            return (null, error);

        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return (null, "Username must be at least 3 characters.");

        if (request.Username.Length > 30)
            return (null, "Username must be at most 30 characters.");

        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return (null, "An account with this email already exists.");

        if (await _userRepository.ExistsByUsernameAsync(request.Username))
            return (null, "This username is already taken.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordService.Hash(request.Password),
            Role = "citizen"
        };

        var createdUser = await _userRepository.CreateAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(createdUser);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        await _refreshTokenRepository.CreateAsync(new Entities.RefreshToken
        {
            UserId = createdUser.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        return (new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = MapToDto(createdUser)
        }, null);
    }

    public async Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordService.Verify(request.Password, user.PasswordHash))
            return (null, "Invalid email or password.");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        await _refreshTokenRepository.CreateAsync(new Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        return (new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = MapToDto(user)
        }, null);
    }

    public async Task<(AuthResponse? Response, string? Error)> RefreshTokenAsync(string token)
    {
        var tokenHash = _tokenService.HashToken(token);
        var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(tokenHash);

        if (refreshTokenEntity is null || !refreshTokenEntity.IsActive)
        {
            return (null, "Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(refreshTokenEntity.UserId);
        if (user is null)
        {
            return (null, "User not found.");
        }

        // Revoke the old token (token rotation)
        refreshTokenEntity.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshTokenEntity);

        // Generate new tokens
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokenRepository.CreateAsync(new Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        return (new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            User = MapToDto(user)
        }, null);
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        var tokenHash = _tokenService.HashToken(token);
        var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(tokenHash);

        if (refreshTokenEntity is null || !refreshTokenEntity.IsActive)
            return false;

        refreshTokenEntity.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshTokenEntity);
        return true;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id.ToString(),
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        Points = user.Points
    };
}
