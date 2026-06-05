using API.Endpoints;
using API.Middleware;

namespace API.Extensions;
public static class WebApplicationExtensions
{
    public static WebApplication UseCleanMapApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors(CleanMapCorsPolicyNames.CleanMap);
        app.UseMiddleware<CleanMapExceptionMiddleware>();
        app.MapCleanMapEndpoints();

        return app;
    }
}
