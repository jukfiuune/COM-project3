using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests;

public sealed class CsrfMiddlewareTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task InvokeAsync_PassesThrough_SafeMethods(string method)
    {
        var nextCalled = false;
        var middleware = new API.Middleware.CsrfMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<API.Middleware.CsrfMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = method;

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task InvokeAsync_Rejects_WhenNoToken(string method)
    {
        var nextCalled = false;
        var middleware = new API.Middleware.CsrfMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<API.Middleware.CsrfMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = method;

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Rejects_WhenTokensMismatch()
    {
        var nextCalled = false;
        var middleware = new API.Middleware.CsrfMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<API.Middleware.CsrfMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["X-CSRF-Token"] = "header-token";
        context.Request.Headers["Cookie"] = "csrfToken=cookie-token";

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Passes_WhenTokensMatch()
    {
        var nextCalled = false;
        var middleware = new API.Middleware.CsrfMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<API.Middleware.CsrfMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["X-CSRF-Token"] = "valid-token";
        context.Request.Headers["Cookie"] = "csrfToken=valid-token";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.NotEqual(403, context.Response.StatusCode);
    }
}
