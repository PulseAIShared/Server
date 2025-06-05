using Application.Abstractions.Data;
using Domain.Customers;
using Domain.Integration;
using Infrastructure.Integrations.Models;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using System.Text.Json;
using System;
using DataTransferObjects.Integrations;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Integrations.Services.Interfaces;
using Application.Services;

namespace Infrastructure.Integrations.Services;

public class HubSpotIntegrationService : IIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HubSpotIntegrationService> _logger;
    private readonly IApplicationDbContext _context;
    private readonly ICustomerAggregationService _customerAggregationService;

    public HubSpotIntegrationService(
        HttpClient httpClient,
        ILogger<HubSpotIntegrationService> logger,
        IApplicationDbContext context,
        ICustomerAggregationService customerAggregationService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _context = context;
        _customerAggregationService = customerAggregationService;
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
                    var sourceData = MapHubSpotContactToSourceData(contact);
                    var email = sourceData.GetValueOrDefault("email")?.ToString();

                    if (string.IsNullOrEmpty(email))
                    {
                        result.ErrorRecords++;
                        result.Errors.Add(new SyncError
                        {
                            RecordId = contact.Id,
                            Message = "Contact has no email address"
                        });
                        continue;
                    }

                    // Find existing customer by email
                    var existingCustomer = existingCustomers.FirstOrDefault(c =>
                        c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                    if (existingCustomer != null)
                    {
                        // Update existing customer using aggregation service
                        await _customerAggregationService.AddOrUpdateCustomerDataAsync(
                            existingCustomer.Id,
                            sourceData,
                            "hubspot",
                            null, // No import batch for integrations
                            null  // No user for automated sync
                        );
                        result.UpdatedRecords++;
                    }
                    else
                    {
                        // Create new customer
                        var newCustomer = new Customer
                        {
                            CompanyId = integration.CompanyId,
                            Email = email,
                            FirstName = sourceData.GetValueOrDefault("firstname")?.ToString() ?? "",
                            LastName = sourceData.GetValueOrDefault("lastname")?.ToString() ?? "",
                            Phone = sourceData.GetValueOrDefault("phone")?.ToString(),
                            CompanyName = sourceData.GetValueOrDefault("company")?.ToString(),
                            JobTitle = sourceData.GetValueOrDefault("jobtitle")?.ToString(),
                            LastSyncedAt = DateTime.UtcNow,
                            ChurnRiskLevel = ChurnRiskLevel.Low,
                            ChurnRiskScore = 0
                        };

                        // Set defaults for required fields if not provided
                        if (string.IsNullOrEmpty(newCustomer.FirstName) && string.IsNullOrEmpty(newCustomer.LastName))
                        {
                            newCustomer.FirstName = "Unknown";
                            newCustomer.LastName = "Contact";
                        }

                        _context.Customers.Add(newCustomer);
                        await _context.SaveChangesAsync(); // Save to get the ID

                        // Now add the HubSpot-specific data using aggregation service
                        await _customerAggregationService.AddOrUpdateCustomerDataAsync(
                            newCustomer.Id,
                            sourceData,
                            "hubspot",
                            null,
                            null
                        );

                        result.NewRecords++;
                        existingCustomers.Add(newCustomer); // Add to local cache
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

            // Update integration sync status
            integration.LastSyncedAt = DateTime.UtcNow;
            integration.SyncedRecordCount = result.ProcessedRecords;
            integration.Status = IntegrationStatus.Connected;
            integration.LastSyncError = null;

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
            "hs_lead_status", "hubspot_owner_id", "lead_source"
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
        var emails = contacts
            .Select(c => c.Properties.Email?.ToLower())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();

        if (!emails.Any())
            return new List<Customer>();

        return await _context.Customers
            .Where(c => c.CompanyId == companyId && emails.Contains(c.Email.ToLower()))
            .ToListAsync();
    }

    private Dictionary<string, object> MapHubSpotContactToSourceData(HubSpotContact contact)
    {
        var sourceData = new Dictionary<string, object>
        {
            ["id"] = contact.Id,
            ["email"] = contact.Properties.Email ?? "",
            ["firstname"] = contact.Properties.Firstname ?? "",
            ["lastname"] = contact.Properties.Lastname ?? "",
            ["phone"] = contact.Properties.Phone ?? "",
            ["company"] = contact.Properties.Company ?? "",
            ["jobtitle"] = contact.Properties.Jobtitle ?? "",
            ["lifecyclestage"] = contact.Properties.Lifecyclestage ?? "",
            ["lead_source"] = contact.Properties.Hs_lead_status ?? "",
            ["sales_owner_name"] = "", // HubSpot doesn't provide owner name directly
        };

        // Parse dates safely
        if (DateTime.TryParse(contact.Properties.Lastmodifieddate?.ToString(), out var lastModified))
        {
            sourceData["last_activity_date"] = lastModified;
        }

        if (DateTime.TryParse(contact.CreatedAt.ToString(), out var createdAt))
        {
            sourceData["first_contact_date"] = createdAt;
        }

        // Set some default CRM values
        sourceData["deal_count"] = 0;
        sourceData["total_deal_value"] = 0;

        return sourceData;
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