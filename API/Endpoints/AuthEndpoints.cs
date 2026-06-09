using System.Security.Claims;
using Core.DTOs;
using Core.Services;
using Microsoft.AspNetCore.Authorization;

namespace API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/signup", HandleSignup);
        group.MapPost("/login", HandleLogin);
        group.MapPost("/refresh", HandleRefresh);
        group.MapPost("/logout", HandleLogout);
        group.MapGet("/me", HandleMe).RequireAuthorization();
    }

    private static void SetRefreshTokenCookie(HttpContext context, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        context.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private static async Task<IResult> HandleSignup(SignupRequest request, IAuthService authService, HttpContext context)
    {
        var (response, error) = await authService.SignupAsync(request);
        if (error is not null) return Results.BadRequest(new { error });
        
        SetRefreshTokenCookie(context, response!.RefreshToken);
        // Don't send refresh token in body
        response.RefreshToken = string.Empty;
        
        return Results.Ok(response);
    }

    private static async Task<IResult> HandleLogin(LoginRequest request, IAuthService authService, HttpContext context)
    {
        var (response, error) = await authService.LoginAsync(request);
        if (error is not null) return Results.BadRequest(new { error });

        SetRefreshTokenCookie(context, response!.RefreshToken);
        // Don't send refresh token in body
        response.RefreshToken = string.Empty;

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleRefresh(HttpContext context, IAuthService authService)
    {
        var refreshToken = context.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.Unauthorized();

        var (response, error) = await authService.RefreshTokenAsync(refreshToken);
        if (error is not null) return Results.Unauthorized();

        SetRefreshTokenCookie(context, response!.RefreshToken);
        // Don't send refresh token in body
        response.RefreshToken = string.Empty;

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleLogout(HttpContext context, IAuthService authService)
    {
        var refreshToken = context.Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await authService.RevokeTokenAsync(refreshToken);
        }

        context.Response.Cookies.Delete("refreshToken");
        return Results.Ok();
    }

    private static IResult HandleMe(HttpContext httpContext)
    {
        var user = httpContext.User;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(ClaimTypes.Name)
                  ?? user.FindFirstValue("sub");
        var email = user.FindFirstValue(ClaimTypes.Email)
                  ?? user.FindFirstValue("email");
        var username = user.FindFirstValue(ClaimTypes.Name)
                  ?? user.FindFirstValue("username");
        var role = user.FindFirstValue(ClaimTypes.Role) ?? "citizen";

        if (string.IsNullOrWhiteSpace(userId))
            return Results.Unauthorized();

        return Results.Ok(new UserDto
        {
            Id = userId,
            Email = email ?? string.Empty,
            Username = username ?? string.Empty,
            Role = role,
            Points = 0
        });
    }
}
