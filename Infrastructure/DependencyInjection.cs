using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Integrations.Services.Interfaces;
using Infrastructure.Integrations.Services;
using Infrastructure.Services;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices(configuration)
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal()
            .AddSignalR(configuration)
            .AddHangfireServices(configuration);

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        var useAzureStorage = configuration.GetValue<bool>("FileStorage:UseAzure");
        if (useAzureStorage)
        {
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }

        // Import service
        services.AddScoped<ICustomerImportService>(provider =>
       new CustomerImportService(
           provider,
           provider.GetRequiredService<IFileStorageService>(),
           provider.GetRequiredService<IBackgroundJobClient>(), 
           provider.GetRequiredService<ILogger<CustomerImportService>>()));

        services.AddScoped<IImportBackgroundService, ImportBackgroundService>();
        // Notification service
        services.AddScoped<INotificationService, NotificationService>();


        services.AddHttpClient<HubSpotIntegrationService>(client =>
        {
            client.BaseAddress = new Uri("https://api.hubapi.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "PulseAI-Integration/1.0");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Register integration services
        services.AddScoped<HubSpotIntegrationService>();
        services.AddScoped<IIntegrationService, HubSpotIntegrationService>();

        // Integration factory for multiple providers
        services.AddScoped<IIntegrationServiceFactory, IntegrationServiceFactory>();

        // Background job for automatic syncing
        services.AddScoped<IIntegrationSyncService, IntegrationSyncService>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

            // Enable dynamic JSON serialization for Dictionary<string, object> and similar types
            dataSourceBuilder.EnableDynamicJson();

            var dataSource = dataSourceBuilder.Build();

            options.UseNpgsql(dataSource, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                };

                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("jwt"))
                        {
                            context.Token = context.Request.Cookies["jwt"];
                        }
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
    private static IServiceCollection AddSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true; 
        });

        return services;
    }

    private static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Hangfire services
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(configuration.GetConnectionString("Database")),
                new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(10),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    TransactionSynchronisationTimeout = TimeSpan.FromMinutes(5),

                }));

        // Add the processing server as IHostedService
        services.AddHangfireServer(options =>
        {
            options.HeartbeatInterval = TimeSpan.FromSeconds(10);
            options.Queues = new[] { "default", "imports", "notifications" };
            options.WorkerCount = Environment.ProcessorCount * 2;
        });

        return services;
    }

}
