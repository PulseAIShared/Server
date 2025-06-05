// Web.Api/Endpoints/Integrations/HubSpotEndpoints.cs
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Integration;
using Infrastructure.Integrations.Models;
using Infrastructure.Integrations.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Integrations;

internal sealed class HubSpotEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("integrations/hubspot")
            .RequireAuthorization()
            .WithTags(Tags.Integrations);

        // Start OAuth flow
        group.MapPost("connect", StartHubSpotConnection)
            .WithName("StartHubSpotConnection")
            .WithSummary("Start HubSpot OAuth connection flow");

        // Handle OAuth callback
        group.MapPost("callback", HandleHubSpotCallback)
            .WithName("HandleHubSpotCallback")
            .WithSummary("Handle HubSpot OAuth callback");

        // Test existing connection
        group.MapPost("{integrationId:guid}/test", TestHubSpotConnection)
            .WithName("TestHubSpotConnection")
            .WithSummary("Test HubSpot connection");

        // Sync customers
        group.MapPost("{integrationId:guid}/sync", SyncHubSpotCustomers)
            .WithName("SyncHubSpotCustomers")
            .WithSummary("Sync customers from HubSpot");

        // Disconnect integration
        group.MapDelete("{integrationId:guid}", DisconnectHubSpot)
            .WithName("DisconnectHubSpot")
            .WithSummary("Disconnect HubSpot integration");
    }

    private static async Task<IResult> StartHubSpotConnection(
        IUserContext userContext,
        IApplicationDbContext context,
        HubSpotIntegrationService hubSpotService,
         IConfiguration configuration,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user?.CompanyId == null)
            {
                return Results.BadRequest(new { error = "User must be associated with a company" });
            }

            // Check if integration already exists
            var existingIntegration = await context.Integrations
                .FirstOrDefaultAsync(i => i.CompanyId == user.CompanyId && i.Type == IntegrationType.HubSpot, cancellationToken);

            if (existingIntegration != null)
            {
                return Results.Conflict(new { error = "HubSpot integration already exists for this company" });
            }

            // Generate state for OAuth security
            var state = Guid.NewGuid().ToString();

            // Store state in cache/session for verification
            httpContext.Session.SetString($"hubspot_oauth_state_{userContext.UserId}", state);

            var clientId = configuration["Integrations:HubSpot:ClientId"] ??
                                   throw new InvalidOperationException("HubSpot client ID not configured");

            var frontendUrl = configuration["Frontend:BaseUrl"] ?? "https://localhost:3000";
            var redirectUri = $"{frontendUrl}/app/oauth/hubspot/callback";

            var authUrl = await hubSpotService.GetAuthorizationUrlAsync(clientId, redirectUri, state);

            return Results.Ok(new
            {
                AuthorizationUrl = authUrl,
                State = state
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to start HubSpot connection: {ex.Message}");
        }
    }

    private static async Task<IResult> HandleHubSpotCallback(
        [FromBody] HubSpotCallbackRequest request,
        IUserContext userContext,
        IApplicationDbContext context,
        HubSpotIntegrationService hubSpotService,
        IConfiguration configuration, 
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Verify state to prevent CSRF attacks
            var storedState = httpContext.Session.GetString($"hubspot_oauth_state_{userContext.UserId}");
            if (storedState != request.State)
            {
                return Results.BadRequest(new { error = "Invalid state parameter" });
            }

            if (!string.IsNullOrEmpty(request.Error))
            {
                return Results.BadRequest(new { error = $"OAuth error: {request.Error}" });
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user?.CompanyId == null)
            {
                return Results.BadRequest(new { error = "User must be associated with a company" });
            }

            var clientId = configuration["Integrations:HubSpot:ClientId"]!;
            var clientSecret = configuration["Integrations:HubSpot:ClientSecret"]!;

            var frontendUrl = configuration["Frontend:BaseUrl"] ?? "https://localhost:3000";
            var redirectUri = $"{frontendUrl}/app/oauth/hubspot/callback";

            // Exchange code for tokens
            var tokenResponse = await hubSpotService.ExchangeCodeForTokenAsync(
                request.Code, clientId, clientSecret, redirectUri);

            // Create integration record
            var integration = new Integration
            {
                CompanyId = user.CompanyId,
                Type = IntegrationType.HubSpot,
                Name = "HubSpot CRM",
                Status = IntegrationStatus.Connected,
                ConfiguredByUserId = userContext.UserId,
                Credentials = new Dictionary<string, string>
                {
                    ["access_token"] = tokenResponse.AccessToken,
                    ["refresh_token"] = tokenResponse.RefreshToken,
                    ["expires_in"] = tokenResponse.ExpiresIn.ToString(),
                    ["token_type"] = tokenResponse.TokenType
                },
                Configuration = new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["sync_enabled"] = "true",
                    ["auto_sync_interval"] = "24" // hours
                }
            };

            context.Integrations.Add(integration);
            await context.SaveChangesAsync(cancellationToken);

            // Clean up session state
            httpContext.Session.Remove($"hubspot_oauth_state_{userContext.UserId}");

            return Results.Ok(new
            {
                IntegrationId = integration.Id,
                Status = integration.Status.ToString(),
                Message = "HubSpot integration connected successfully"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to complete HubSpot connection: {ex.Message}");
        }
    }

    private static async Task<IResult> TestHubSpotConnection(
        Guid integrationId,
        IUserContext userContext,
        IApplicationDbContext context,
        HubSpotIntegrationService hubSpotService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user?.CompanyId == null)
            {
                return Results.BadRequest(new { error = "User must be associated with a company" });
            }

            var integration = await context.Integrations
                .FirstOrDefaultAsync(i => i.Id == integrationId && i.CompanyId == user.CompanyId, cancellationToken);

            if (integration == null)
            {
                return Results.NotFound(new { error = "Integration not found" });
            }

            var isConnected = await hubSpotService.TestConnectionAsync(integration);

            if (isConnected)
            {
                integration.Status = IntegrationStatus.Connected;
                integration.LastSyncError = null;
            }
            else
            {
                integration.Status = IntegrationStatus.Error;
                integration.LastSyncError = "Connection test failed";
            }

            await context.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                IntegrationId = integration.Id,
                IsConnected = isConnected,
                Status = integration.Status.ToString(),
                LastTested = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to test HubSpot connection: {ex.Message}");
        }
    }

    private static async Task<IResult> SyncHubSpotCustomers(
        Guid integrationId,
        [FromBody] SyncRequest request,
        IUserContext userContext,
        IApplicationDbContext context,
        HubSpotIntegrationService hubSpotService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user?.CompanyId == null)
            {
                return Results.BadRequest(new { error = "User must be associated with a company" });
            }

            var integration = await context.Integrations
                .FirstOrDefaultAsync(i => i.Id == integrationId && i.CompanyId == user.CompanyId, cancellationToken);

            if (integration == null)
            {
                return Results.NotFound(new { error = "Integration not found" });
            }

            if (integration.Status != IntegrationStatus.Connected)
            {
                return Results.BadRequest(new { error = "Integration is not connected" });
            }

            // Set sync status
            integration.Status = IntegrationStatus.Syncing;
            await context.SaveChangesAsync(cancellationToken);

            var syncOptions = new SyncOptions
            {
                IncrementalSync = request.IncrementalSync,
                SyncFromDate = request.SyncFromDate,
                BatchSize = 100
            };

            var result = await hubSpotService.SyncCustomersAsync(integration, syncOptions);

            return Results.Ok(new
            {
                SyncId = Guid.NewGuid(),
                IntegrationId = integration.Id,
                StartTime = result.StartTime,
                EndTime = result.EndTime,
                TotalRecords = result.TotalRecords,
                ProcessedRecords = result.ProcessedRecords,
                NewRecords = result.NewRecords,
                UpdatedRecords = result.UpdatedRecords,
                ErrorRecords = result.ErrorRecords,
                Errors = result.Errors.Take(10), // Limit errors in response
                Message = $"Sync completed. Processed {result.ProcessedRecords} records, {result.NewRecords} new, {result.UpdatedRecords} updated"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to sync HubSpot customers: {ex.Message}");
        }
    }

    private static async Task<IResult> DisconnectHubSpot(
        Guid integrationId,
        IUserContext userContext,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user?.CompanyId == null)
            {
                return Results.BadRequest(new { error = "User must be associated with a company" });
            }

            var integration = await context.Integrations
                .FirstOrDefaultAsync(i => i.Id == integrationId && i.CompanyId == user.CompanyId, cancellationToken);

            if (integration == null)
            {
                return Results.NotFound(new { error = "Integration not found" });
            }

            integration.Status = IntegrationStatus.Disconnected;
            integration.Credentials = null; // Clear sensitive data

            await context.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                IntegrationId = integration.Id,
                Status = integration.Status.ToString(),
                Message = "HubSpot integration disconnected successfully"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to disconnect HubSpot integration: {ex.Message}");
        }
    }
}

// Request/Response models
public sealed record HubSpotCallbackRequest(
    string Code,
    string State,
    string? Error = null
);

public sealed record SyncRequest(
    bool IncrementalSync = true,
    DateTime? SyncFromDate = null
);