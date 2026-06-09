using Core.DTOs;

namespace Core.Services;

public interface IAuthService
{
    Task<(AuthResponse? Response, string? Error)> SignupAsync(SignupRequest request);
    Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request);
    Task<(AuthResponse? Response, string? Error)> RefreshTokenAsync(string token);
    Task<bool> RevokeTokenAsync(string token);
}
