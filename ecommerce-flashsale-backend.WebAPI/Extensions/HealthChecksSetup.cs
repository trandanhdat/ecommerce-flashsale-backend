using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce_flashsale_backend.Extensions
{
    public static class HealthChecksSetup
    {
        public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MyDB") ?? "";
            var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
            
            services.AddHealthChecks()
                .AddSqlServer(connectionString, name: "SQL Server")
                .AddRedis(redisConnectionString, name: "Redis");
                
            return services;
        }
    }
}
