// Web.Api/DependencyInjection.cs - Fixed version
using Web.Api.Infrastructure;

namespace Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddMemoryCache();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Get environment from configuration
        var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";

        services.AddCors(options =>
        {
            if (isDevelopment)
            {
                // Development CORS - more permissive for local testing
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(
                        "https://localhost:3000",
                        "http://localhost:3000",
                        "https://localhost:5173", // Vite dev server
                        "http://localhost:5173"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            }
            else
            {
                // Production CORS - specific origins only
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(
                        "https://pulseretention.netlify.app",
                        "https://pulseai-api.ddns.net", // Add your own API domain
                        "http://167.71.17.59",
                        "https://167.71.17.59"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight for 10 minutes
                });
            }

            // Add a fallback policy for troubleshooting
            options.AddPolicy("Permissive", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}