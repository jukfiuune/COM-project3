using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Middleware;

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfMiddleware> _logger;

    public CsrfMiddleware(RequestDelegate next, ILogger<CsrfMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) || 
            HttpMethods.IsPut(context.Request.Method) || 
            HttpMethods.IsPatch(context.Request.Method) || 
            HttpMethods.IsDelete(context.Request.Method))
        {
            var headerToken = context.Request.Headers["X-CSRF-Token"].ToString();
            var cookieToken = context.Request.Cookies["csrfToken"];

            if (string.IsNullOrEmpty(headerToken) || string.IsNullOrEmpty(cookieToken) || headerToken != cookieToken)
            {
                _logger.LogWarning("CSRF validation failed.");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid CSRF token." });
                return;
            }
        }
        else if (HttpMethods.IsGet(context.Request.Method))
        {
            if (!context.Request.Cookies.ContainsKey("csrfToken"))
            {
                var token = System.Guid.NewGuid().ToString("N");
                context.Response.Cookies.Append("csrfToken", token, new CookieOptions
                {
                    HttpOnly = false, // Must be readable by JS
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Strict
                });
            }
        }

        await _next(context);
    }
}
