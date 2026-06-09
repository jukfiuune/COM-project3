using MongoDB.Driver;

namespace API.Middleware;

public sealed class CleanMapExceptionMiddleware(
    RequestDelegate next,
    ILogger<CleanMapExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(
                context,
                StatusCodes.Status409Conflict,
                "A report with this id already exists.");
        }
        catch (MongoException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogError(ex, "MongoDB request failed.");

            await WriteErrorAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "MongoDB is not reachable.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string error)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { error });
    }
}
