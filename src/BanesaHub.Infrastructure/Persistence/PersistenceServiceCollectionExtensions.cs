using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BanesaHub.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddBanesaHubPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BanesaHubDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("BanesaHub");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Configure ConnectionStrings:BanesaHub before using persistence.");
            }

            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
