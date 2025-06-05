using Application.Abstractions.Data;
using Domain.Customers;
using Domain.Integration;
using Infrastructure.Integrations.Models;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using System.Text.Json;
using System;
using DataTransferObjects.Integrations;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Integrations.Services.Interfaces;


namespace Infrastructure.Integrations.Services;

public class HubSpotIntegrationService : IIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HubSpotIntegrationService> _logger;
    private readonly IApplicationDbContext _context;

    public HubSpotIntegrationService(
        HttpClient httpClient,
        ILogger<HubSpotIntegrationService> logger,
        IApplicationDbContext context)
    {
        _httpClient = httpClient;
        _logger = logger;
        _context = context;
    }

    public IntegrationType Type => IntegrationType.HubSpot;

    public async Task<bool> TestConnectionAsync(Integration integration)
    {
        try
        {
            var accessToken = GetAccessToken(integration);
            if (string.IsNullOrEmpty(accessToken))
                return false;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync("https://api.hubapi.com/crm/v3/objects/contacts?limit=1");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test HubSpot connection for integration {IntegrationId}", integration.Id);
            return false;
        }
    }

    public async Task<SyncResult> SyncCustomersAsync(Integration integration, SyncOptions options)
    {
        var result = new SyncResult
        {
            StartTime = DateTime.UtcNow,
            TotalRecords = 0,
            ProcessedRecords = 0,
            NewRecords = 0,
            UpdatedRecords = 0,
            ErrorRecords = 0,
            Errors = new List<SyncError>()
        };

        try
        {
            var accessToken = GetAccessToken(integration);
            if (string.IsNullOrEmpty(accessToken))
            {
                result.Errors.Add(new SyncError { Message = "Access token not found" });
                return result;
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var contacts = await FetchAllContactsAsync(integration, options);
            result.TotalRecords = contacts.Count;

            var existingCustomers = await GetExistingCustomersAsync(integration.CompanyId, contacts);

            foreach (var contact in contacts)
            {
                try
                {
                    var customer = MapHubSpotContactToCustomer(contact, integration);

                    var existingCustomer = existingCustomers.FirstOrDefault(c =>
                        c.Email.Equals(customer.Email, StringComparison.OrdinalIgnoreCase) ||
                        c.ExternalId == customer.ExternalId);

                    if (existingCustomer != null)
                    {
                        // Update existing customer
                        if (UpdateCustomerFromHubSpot(existingCustomer, customer))
                        {
                            result.UpdatedRecords++;
                        }
                    }
                    else
                    {
                        // Create new customer
                        customer.CompanyId = integration.CompanyId;
                        customer.Source = "hubspot";
                        customer.LastSyncedAt = DateTime.UtcNow;

                        _context.Customers.Add(customer);
                        result.NewRecords++;
                    }

                    result.ProcessedRecords++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process HubSpot contact {ContactId}", contact.Id);
                    result.ErrorRecords++;
                    result.Errors.Add(new SyncError
                    {
                        RecordId = contact.Id,
                        Message = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Update integration sync status
            integration.LastSyncedAt = DateTime.UtcNow;
            integration.SyncedRecordCount = result.ProcessedRecords;
            integration.Status = IntegrationStatus.Connected;

            await _context.SaveChangesAsync();

            result.EndTime = DateTime.UtcNow;
            _logger.LogInformation("HubSpot sync completed for integration {IntegrationId}. " +
                "Processed: {Processed}, New: {New}, Updated: {Updated}, Errors: {Errors}",
                integration.Id, result.ProcessedRecords, result.NewRecords, result.UpdatedRecords, result.ErrorRecords);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HubSpot sync failed for integration {IntegrationId}", integration.Id);
            result.Errors.Add(new SyncError { Message = $"Sync failed: {ex.Message}" });

            integration.Status = IntegrationStatus.Error;
            integration.LastSyncError = ex.Message;
            await _context.SaveChangesAsync();

            return result;
        }
    }

    private async Task<List<HubSpotContact>> FetchAllContactsAsync(Integration integration, SyncOptions options)
    {
        var allContacts = new List<HubSpotContact>();
        var baseUrl = "https://api.hubapi.com/crm/v3/objects/contacts";

        // Properties we want to fetch
        var properties = new[]
        {
            "email", "firstname", "lastname", "phone", "company", "jobtitle",
            "lifecyclestage", "createdate", "lastmodifieddate", "last_activity_date",
            "hs_lead_status", "hubspot_owner_id"
        };

        var url = $"{baseUrl}?properties={string.Join(",", properties)}&limit=100";

        // Add date filter for incremental sync
        if (options.IncrementalSync && integration.LastSyncedAt.HasValue)
        {
            var lastSync = integration.LastSyncedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            url += $"&filterGroups=[{{\"filters\":[{{\"propertyName\":\"lastmodifieddate\",\"operator\":\"GTE\",\"value\":\"{lastSync}\"}}]}}]";
        }

        string? after = null;
        do
        {
            var requestUrl = after != null ? $"{url}&after={after}" : url;

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<HubSpotContactsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Results != null)
            {
                allContacts.AddRange(result.Results);
            }

            after = result?.Paging?.Next?.After;

        } while (!string.IsNullOrEmpty(after));

        return allContacts;
    }

    private async Task<List<Customer>> GetExistingCustomersAsync(Guid companyId, List<HubSpotContact> contacts)
    {
        var emails = contacts.Select(c => c.Properties.Email.ToLower()).Where(e => !string.IsNullOrEmpty(e)).ToList();
        var externalIds = contacts.Select(c => c.Id).ToList();

        return await _context.Customers
            .Where(c => c.CompanyId == companyId &&
                       (emails.Contains(c.Email.ToLower()) || externalIds.Contains(c.ExternalId)))
            .ToListAsync();
    }

    private Customer MapHubSpotContactToCustomer(HubSpotContact contact, Integration integration)
    {
        var customer = new Customer
        {
            ExternalId = contact.Id,
            Source = "hubspot",
            FirstName = contact.Properties.Firstname ?? "",
            LastName = contact.Properties.Lastname ?? "",
            Email = contact.Properties.Email ?? "",
            Phone = contact.Properties.Phone,
            CompanyName = contact.Properties.Company,
            JobTitle = contact.Properties.Jobtitle,
            LastSyncedAt = DateTime.UtcNow,

            // Map HubSpot lifecycle stage to our subscription status
            SubscriptionStatus = MapLifecycleStageToSubscriptionStatus(contact.Properties.Lifecyclestage),

            // Set defaults
            Plan = SubscriptionPlan.Trial,
            PaymentStatus = PaymentStatus.Active,
            ChurnRiskLevel = ChurnRiskLevel.Low,
            ChurnRiskScore = 0
        };

        // Parse dates
        if (DateTime.TryParse(contact.Properties.Lastmodifieddate?.ToString(), out var lastModified))
        {
            customer.LastLoginDate = lastModified;
        }

        return customer;
    }

    private bool UpdateCustomerFromHubSpot(Customer existing, Customer updated)
    {
        bool hasChanges = false;

        if (existing.FirstName != updated.FirstName)
        {
            existing.FirstName = updated.FirstName;
            hasChanges = true;
        }

        if (existing.LastName != updated.LastName)
        {
            existing.LastName = updated.LastName;
            hasChanges = true;
        }

        if (existing.Phone != updated.Phone)
        {
            existing.Phone = updated.Phone;
            hasChanges = true;
        }

        if (existing.CompanyName != updated.CompanyName)
        {
            existing.CompanyName = updated.CompanyName;
            hasChanges = true;
        }

        if (existing.JobTitle != updated.JobTitle)
        {
            existing.JobTitle = updated.JobTitle;
            hasChanges = true;
        }

        if (hasChanges)
        {
            existing.LastSyncedAt = DateTime.UtcNow;
        }

        return hasChanges;
    }

    private SubscriptionStatus MapLifecycleStageToSubscriptionStatus(string? lifecycleStage)
    {
        return lifecycleStage?.ToLower() switch
        {
            "lead" => SubscriptionStatus.Trial,
            "marketingqualifiedlead" => SubscriptionStatus.Trial,
            "salesqualifiedlead" => SubscriptionStatus.Trial,
            "opportunity" => SubscriptionStatus.Trial,
            "customer" => SubscriptionStatus.Active,
            "evangelist" => SubscriptionStatus.Active,
            _ => SubscriptionStatus.Trial
        };
    }

    private string? GetAccessToken(Integration integration)
    {
        return integration.Credentials?.GetValueOrDefault("access_token");
    }

    public async Task<string> GetAuthorizationUrlAsync(string clientId, string redirectUri, string state)
    {
        var scopes = "crm.objects.contacts.read crm.objects.companies.read";
        var authUrl = $"https://app.hubspot.com/oauth/authorize" +
                     $"?client_id={clientId}" +
                     $"&scope={Uri.EscapeDataString(scopes)}" +
                     $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                     $"&state={state}";

        return authUrl;
    }

    public async Task<Models.OAuthTokenResponse> ExchangeCodeForTokenAsync(string code, string clientId, string clientSecret, string redirectUri)
    {
        // HubSpot OAuth token endpoint expects form-encoded data, not JSON
        var tokenRequest = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("client_id", clientId),
        new("client_secret", clientSecret),
        new("redirect_uri", redirectUri),
        new("code", code)
    };

        var content = new FormUrlEncodedContent(tokenRequest);

        var response = await _httpClient.PostAsync("https://api.hubapi.com/oauth/v1/token", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to exchange code for token: {responseContent}");
        }

        return JsonSerializer.Deserialize<Models.OAuthTokenResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
