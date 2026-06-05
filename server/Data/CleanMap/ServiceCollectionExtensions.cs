using Core.CleanMap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.CleanMap;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleanMapMongo(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CleanMapMongoOptions>(configuration.GetSection(CleanMapMongoOptions.SectionName));
        services.AddSingleton<MongoCleanMapReportRepository>();
        services.AddSingleton<ICleanMapReportRepository>(provider =>
            provider.GetRequiredService<MongoCleanMapReportRepository>());
        services.AddSingleton<ICleanMapHealthCheck>(provider =>
            provider.GetRequiredService<MongoCleanMapReportRepository>());

        return services;
    }
}
