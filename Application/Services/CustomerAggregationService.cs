using Application.Abstractions.Data;
using DataTransferObjects.Customers;
using Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CustomerAggregationService : ICustomerAggregationService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CustomerAggregationService> _logger;

        // Source priority mapping (higher = more trusted)
        private readonly Dictionary<string, int> _sourcePriorities = new()
        {
            ["manual_import"] = 100,     // Manual imports are most trusted
            ["stripe"] = 90,             // Payment processors are highly trusted
            ["paypal"] = 85,
            ["salesforce"] = 80,         // Enterprise CRMs are trusted
            ["hubspot"] = 75,
            ["pipedrive"] = 70,
            ["mailchimp"] = 60,          // Marketing tools are moderately trusted
            ["klaviyo"] = 60,
            ["zendesk"] = 60,            // Support tools
            ["intercom"] = 55,
            ["google_analytics"] = 50,   // Analytics tools
            ["mixpanel"] = 50,
            ["unknown"] = 10             // Default for unknown sources
        };

        public CustomerAggregationService(
            IApplicationDbContext context,
            ILogger<CustomerAggregationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CustomerResponse> GetUnifiedCustomerAsync(Guid customerId, Guid companyId)
        {
            var customer = await _context.Customers
                .Include(c => c.CrmDataSources.Where(cds => cds.IsActive))
                .Include(c => c.PaymentDataSources.Where(pds => pds.IsActive))
                .Include(c => c.MarketingDataSources.Where(mds => mds.IsActive))
                .Include(c => c.SupportDataSources.Where(sds => sds.IsActive))
                .Include(c => c.EngagementDataSources.Where(eds => eds.IsActive))
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer == null)
                throw new ArgumentException($"Customer {customerId} not found");

            return await MapToUnifiedResponse(customer);
        }

        public async Task<Customer> AddOrUpdateCustomerDataAsync(
            Guid customerId,
            Dictionary<string, object> sourceData,
            string sourceName,
            string? importBatchId = null,
            Guid? importedByUserId = null)
        {
            var customer = await _context.Customers
                .Include(c => c.CrmDataSources)
                .Include(c => c.PaymentDataSources)
                .Include(c => c.MarketingDataSources)
                .Include(c => c.SupportDataSources)
                .Include(c => c.EngagementDataSources)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
                throw new ArgumentException($"Customer {customerId} not found");

            // Determine data type based on source or data content
            var dataType = DetermineDataType(sourceName, sourceData);

            switch (dataType.ToLower())
            {
                case "crm":
                    await AddOrUpdateCrmDataAsync(customer, sourceData, sourceName, importBatchId, importedByUserId);
                    break;
                case "payment":
                    await AddOrUpdatePaymentDataAsync(customer, sourceData, sourceName, importBatchId, importedByUserId);
                    break;
                case "marketing":
                    await AddOrUpdateMarketingDataAsync(customer, sourceData, sourceName, importBatchId, importedByUserId);
                    break;
                case "support":
                    await AddOrUpdateSupportDataAsync(customer, sourceData, sourceName, importBatchId, importedByUserId);
                    break;
                case "engagement":
                    await AddOrUpdateEngagementDataAsync(customer, sourceData, sourceName, importBatchId, importedByUserId);
                    break;
                default:
                    _logger.LogWarning("Unknown data type {DataType} for source {SourceName}", dataType, sourceName);
                    break;
            }

            // Update core customer fields using conflict resolution
            await UpdateCoreCustomerFieldsWithConflictResolution(customer, sourceData, sourceName);

            // Recalculate aggregated metrics
            await RecalculateCustomerMetricsAsync(customer);

            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> SetPrimarySourceAsync(Guid customerId, string dataType, string sourceName)
        {
            var customer = await _context.Customers
                .Include(c => c.CrmDataSources)
                .Include(c => c.PaymentDataSources)
                .Include(c => c.MarketingDataSources)
                .Include(c => c.SupportDataSources)
                .Include(c => c.EngagementDataSources)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
                throw new ArgumentException($"Customer {customerId} not found");

            switch (dataType.ToLower())
            {
                case "crm":
                    if (customer.CrmDataSources.Any(c => c.Source == sourceName && c.IsActive))
                    {
                        // Update all CRM sources to not be primary
                        foreach (var crmData in customer.CrmDataSources)
                            crmData.IsPrimarySource = false;

                        // Set the specified source as primary
                        var primaryCrm = customer.CrmDataSources.First(c => c.Source == sourceName);
                        primaryCrm.IsPrimarySource = true;
                        customer.PrimaryCrmSource = sourceName;
                    }
                    break;

                case "payment":
                    if (customer.PaymentDataSources.Any(p => p.Source == sourceName && p.IsActive))
                    {
                        foreach (var paymentData in customer.PaymentDataSources)
                            paymentData.IsPrimarySource = false;

                        var primaryPayment = customer.PaymentDataSources.First(p => p.Source == sourceName);
                        primaryPayment.IsPrimarySource = true;
                        customer.PrimaryPaymentSource = sourceName;
                    }
                    break;

                    // Similar for other data types...
            }

            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> DeactivateSourceAsync(Guid customerId, string dataType, string sourceName)
        {
            var customer = await _context.Customers
                .Include(c => c.CrmDataSources)
                .Include(c => c.PaymentDataSources)
                .Include(c => c.MarketingDataSources)
                .Include(c => c.SupportDataSources)
                .Include(c => c.EngagementDataSources)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null) return false;

            bool deactivated = false;

            switch (dataType.ToLower())
            {
                case "crm":
                    var crmData = customer.CrmDataSources.FirstOrDefault(c => c.Source == sourceName);
                    if (crmData != null)
                    {
                        crmData.IsActive = false;
                        deactivated = true;

                        // If this was the primary source, auto-select a new primary
                        if (customer.PrimaryCrmSource == sourceName)
                        {
                            var newPrimary = customer.CrmDataSources
                                .Where(c => c.IsActive)
                                .OrderByDescending(c => _sourcePriorities.GetValueOrDefault(c.Source, 10))
                                .ThenByDescending(c => c.LastSyncedAt)
                                .FirstOrDefault();

                            customer.PrimaryCrmSource = newPrimary?.Source;
                            if (newPrimary != null)
                                newPrimary.IsPrimarySource = true;
                        }
                    }
                    break;

                case "payment":
                    var paymentData = customer.PaymentDataSources.FirstOrDefault(p => p.Source == sourceName);
                    if (paymentData != null)
                    {
                        paymentData.IsActive = false;
                        deactivated = true;

                        if (customer.PrimaryPaymentSource == sourceName)
                        {
                            var newPrimary = customer.PaymentDataSources
                                .Where(p => p.IsActive)
                                .OrderByDescending(p => _sourcePriorities.GetValueOrDefault(p.Source, 10))
                                .ThenByDescending(p => p.LastSyncedAt)
                                .FirstOrDefault();

                            customer.PrimaryPaymentSource = newPrimary?.Source;
                            if (newPrimary != null)
                                newPrimary.IsPrimarySource = true;
                        }
                    }
                    break;
            }

            if (deactivated)
            {
                await _context.SaveChangesAsync();
            }

            return deactivated;
        }

        public async Task<List<CustomerCrmInfo>> GetAllCrmSourcesAsync(Guid customerId)
        {
            var crmDataSources = await _context.CustomerCrmData
                .Include(cd => cd.ImportedBy)
                .Where(cd => cd.CustomerId == customerId && cd.IsActive)
                .OrderByDescending(cd => cd.IsPrimarySource)
                .ThenByDescending(cd => cd.LastSyncedAt)
                .ToListAsync();

            return crmDataSources.Select(cd => new CustomerCrmInfo
            {
                Source = cd.Source,
                IsPrimarySource = cd.IsPrimarySource,
                ImportBatchId = cd.ImportBatchId,
                ImportedByUserName = cd.ImportedBy != null ? $"{cd.ImportedBy.FirstName} {cd.ImportedBy.LastName}" : null,
                LeadSource = cd.LeadSource,
                LifecycleStage = cd.LifecycleStage,
                SalesOwnerName = cd.SalesOwnerName,
                LastActivityDate = cd.LastActivityDate,
                DealCount = cd.DealCount,
                TotalDealValue = cd.TotalDealValue,
                LastSyncedAt = cd.LastSyncedAt,
                IsActive = cd.IsActive
            }).ToList();
        }

        public async Task<List<CustomerPaymentInfo>> GetAllPaymentSourcesAsync(Guid customerId)
        {
            var paymentDataSources = await _context.CustomerPaymentData
                .Include(pd => pd.ImportedBy)
                .Where(pd => pd.CustomerId == customerId && pd.IsActive)
                .OrderByDescending(pd => pd.IsPrimarySource)
                .ThenByDescending(pd => pd.LastSyncedAt)
                .ToListAsync();

            return paymentDataSources.Select(pd => new CustomerPaymentInfo
            {
                Source = pd.Source,
                IsPrimarySource = pd.IsPrimarySource,
                ImportBatchId = pd.ImportBatchId,
                ImportedByUserName = pd.ImportedBy != null ? $"{pd.ImportedBy.FirstName} {pd.ImportedBy.LastName}" : null,
                SubscriptionStatus = pd.SubscriptionStatus,
                Plan = pd.Plan,
                MonthlyRecurringRevenue = pd.MonthlyRecurringRevenue,
                LifetimeValue = pd.LifetimeValue,
                SubscriptionStartDate = pd.SubscriptionStartDate,
                PaymentStatus = pd.PaymentStatus,
                NextBillingDate = pd.NextBillingDate,
                PaymentFailureCount = pd.PaymentFailureCount,
                LastSyncedAt = pd.LastSyncedAt,
                IsActive = pd.IsActive
            }).ToList();
        }

        public async Task<decimal> CalculateAggregatedChurnRiskAsync(Guid customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.PaymentDataSources.Where(p => p.IsActive))
                .Include(c => c.EngagementDataSources.Where(e => e.IsActive))
                .Include(c => c.SupportDataSources.Where(s => s.IsActive))
                .Include(c => c.MarketingDataSources.Where(m => m.IsActive))
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null) return 0;

            decimal totalRisk = 0;
            decimal totalWeight = 0;

            // Aggregate payment risk from all active payment sources
            foreach (var paymentData in customer.PaymentDataSources)
            {
                var weight = _sourcePriorities.GetValueOrDefault(paymentData.Source, 10) / 100m;
                var risk = CalculatePaymentRisk(paymentData);
                totalRisk += risk * weight * 0.4m; // 40% weight for payment data
                totalWeight += weight * 0.4m;
            }

            // Aggregate engagement risk from all active engagement sources
            foreach (var engagementData in customer.EngagementDataSources)
            {
                var weight = _sourcePriorities.GetValueOrDefault(engagementData.Source, 10) / 100m;
                var risk = CalculateEngagementRisk(engagementData);
                totalRisk += risk * weight * 0.3m; // 30% weight for engagement data
                totalWeight += weight * 0.3m;
            }

            // Similar for support and marketing data...

            // Normalize by total weight to get final score
            var finalRisk = totalWeight > 0 ? totalRisk / totalWeight : 0;
            return Math.Min(100, Math.Max(0, finalRisk * 100));
        }

        // Private helper methods

        private async Task AddOrUpdateCrmDataAsync(
            Customer customer,
            Dictionary<string, object> sourceData,
            string sourceName,
            string? importBatchId,
            Guid? importedByUserId)
        {
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ?? "";

            // Find existing CRM data for this source
            var existingCrmData = customer.CrmDataSources
                .FirstOrDefault(c => c.Source == sourceName && (c.ExternalId == externalId || string.IsNullOrEmpty(externalId)));

            if (existingCrmData != null)
            {
                // Update existing data
                UpdateCrmDataFields(existingCrmData, sourceData);
                existingCrmData.LastSyncedAt = DateTime.UtcNow;
                existingCrmData.ImportBatchId = importBatchId;
            }
            else
            {
                // Create new CRM data entry
                var newCrmData = new CustomerCrmData
                {
                    CustomerId = customer.Id,
                    Source = sourceName,
                    ExternalId = externalId,
                    SourcePriority = _sourcePriorities.GetValueOrDefault(sourceName, 10),
                    ImportBatchId = importBatchId,
                    ImportedByUserId = importedByUserId,
                    LastSyncedAt = DateTime.UtcNow,
                    IsActive = true
                };

                UpdateCrmDataFields(newCrmData, sourceData);

                // Set as primary if this is the first CRM source or has higher priority
                var currentPrimary = customer.CrmDataSources.FirstOrDefault(c => c.IsPrimarySource && c.IsActive);
                if (currentPrimary == null || newCrmData.SourcePriority > currentPrimary.SourcePriority)
                {
                    if (currentPrimary != null)
                        currentPrimary.IsPrimarySource = false;

                    newCrmData.IsPrimarySource = true;
                    customer.PrimaryCrmSource = sourceName;
                }

                customer.CrmDataSources.Add(newCrmData);
                _context.CustomerCrmData.Add(newCrmData);
            }
        }

        private async Task AddOrUpdatePaymentDataAsync(
            Customer customer,
            Dictionary<string, object> sourceData,
            string sourceName,
            string? importBatchId,
            Guid? importedByUserId)
        {
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ?? "";

            var existingPaymentData = customer.PaymentDataSources
                .FirstOrDefault(p => p.Source == sourceName && (p.ExternalId == externalId || string.IsNullOrEmpty(externalId)));

            if (existingPaymentData != null)
            {
                UpdatePaymentDataFields(existingPaymentData, sourceData);
                existingPaymentData.LastSyncedAt = DateTime.UtcNow;
                existingPaymentData.ImportBatchId = importBatchId;
            }
            else
            {
                var newPaymentData = new CustomerPaymentData
                {
                    CustomerId = customer.Id,
                    Source = sourceName,
                    ExternalId = externalId,
                    SourcePriority = _sourcePriorities.GetValueOrDefault(sourceName, 10),
                    ImportBatchId = importBatchId,
                    ImportedByUserId = importedByUserId,
                    LastSyncedAt = DateTime.UtcNow,
                    IsActive = true
                };

                UpdatePaymentDataFields(newPaymentData, sourceData);

                // Set as primary based on priority
                var currentPrimary = customer.PaymentDataSources.FirstOrDefault(p => p.IsPrimarySource && p.IsActive);
                if (currentPrimary == null || newPaymentData.SourcePriority > currentPrimary.SourcePriority)
                {
                    if (currentPrimary != null)
                        currentPrimary.IsPrimarySource = false;

                    newPaymentData.IsPrimarySource = true;
                    customer.PrimaryPaymentSource = sourceName;
                }

                customer.PaymentDataSources.Add(newPaymentData);
                _context.CustomerPaymentData.Add(newPaymentData);
            }
        }

        private async Task UpdateCoreCustomerFieldsWithConflictResolution(
            Customer customer,
            Dictionary<string, object> sourceData,
            string sourceName)
        {
            var sourcePriority = _sourcePriorities.GetValueOrDefault(sourceName, 10);

            // Only update core fields if this source has higher priority than what we currently trust
            // or if the field is currently empty

            if (ShouldUpdateField(customer.FirstName, sourceData.GetValueOrDefault("first_name")?.ToString(), sourcePriority))
                customer.FirstName = sourceData.GetValueOrDefault("first_name")?.ToString() ?? customer.FirstName;

            if (ShouldUpdateField(customer.LastName, sourceData.GetValueOrDefault("last_name")?.ToString(), sourcePriority))
                customer.LastName = sourceData.GetValueOrDefault("last_name")?.ToString() ?? customer.LastName;

            if (ShouldUpdateField(customer.Phone, sourceData.GetValueOrDefault("phone")?.ToString(), sourcePriority))
                customer.Phone = sourceData.GetValueOrDefault("phone")?.ToString();

            // Continue for other core fields...

            customer.LastSyncedAt = DateTime.UtcNow;
        }

        private bool ShouldUpdateField(string? currentValue, string? newValue, int sourcePriority)
        {
            // Always update if current value is empty
            if (string.IsNullOrEmpty(currentValue) && !string.IsNullOrEmpty(newValue))
                return true;

            // Update if new value is not empty and source priority is high enough
            // For simplicity, using priority > 50 as "trusted enough to overwrite"
            if (!string.IsNullOrEmpty(newValue) && sourcePriority > 50)
                return true;

            return false;
        }

        private string DetermineDataType(string sourceName, Dictionary<string, object> sourceData)
        {
            // Try to determine data type from source name first
            var lowerSource = sourceName.ToLower();
            if (lowerSource.Contains("salesforce") || lowerSource.Contains("hubspot") || lowerSource.Contains("pipedrive") || lowerSource.Contains("crm"))
                return "crm";
            if (lowerSource.Contains("stripe") || lowerSource.Contains("paypal") || lowerSource.Contains("payment"))
                return "payment";
            if (lowerSource.Contains("mailchimp") || lowerSource.Contains("klaviyo") || lowerSource.Contains("marketing"))
                return "marketing";
            if (lowerSource.Contains("zendesk") || lowerSource.Contains("intercom") || lowerSource.Contains("support"))
                return "support";
            if (lowerSource.Contains("analytics") || lowerSource.Contains("mixpanel") || lowerSource.Contains("engagement"))
                return "engagement";

            // Try to determine from data content
            if (sourceData.ContainsKey("subscription_status") || sourceData.ContainsKey("mrr") || sourceData.ContainsKey("payment_status"))
                return "payment";
            if (sourceData.ContainsKey("lead_source") || sourceData.ContainsKey("lifecycle_stage") || sourceData.ContainsKey("sales_owner"))
                return "crm";
            if (sourceData.ContainsKey("email_open_rate") || sourceData.ContainsKey("campaign_count"))
                return "marketing";
            if (sourceData.ContainsKey("support_tickets") || sourceData.ContainsKey("csat_score"))
                return "support";
            if (sourceData.ContainsKey("last_login") || sourceData.ContainsKey("feature_usage"))
                return "engagement";

            // Default to CRM if we can't determine
            return "crm";
        }

        // Helper methods for updating specific data types...
        private void UpdateCrmDataFields(CustomerCrmData crmData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("lead_source"))
                crmData.LeadSource = sourceData["lead_source"]?.ToString();
            if (sourceData.ContainsKey("lifecycle_stage"))
                crmData.LifecycleStage = sourceData["lifecycle_stage"]?.ToString();
            if (sourceData.ContainsKey("sales_owner_name"))
                crmData.SalesOwnerName = sourceData["sales_owner_name"]?.ToString();
            if (sourceData.ContainsKey("deal_count") && int.TryParse(sourceData["deal_count"]?.ToString(), out var dealCount))
                crmData.DealCount = dealCount;
            if (sourceData.ContainsKey("total_deal_value") && decimal.TryParse(sourceData["total_deal_value"]?.ToString(), out var totalValue))
                crmData.TotalDealValue = totalValue;
            // ... other CRM fields
        }

        private void UpdatePaymentDataFields(CustomerPaymentData paymentData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("subscription_status") && Enum.TryParse<SubscriptionStatus>(sourceData["subscription_status"]?.ToString(), out var subStatus))
                paymentData.SubscriptionStatus = subStatus;
            if (sourceData.ContainsKey("plan") && Enum.TryParse<SubscriptionPlan>(sourceData["plan"]?.ToString(), out var plan))
                paymentData.Plan = plan;
            if (sourceData.ContainsKey("mrr") && decimal.TryParse(sourceData["mrr"]?.ToString(), out var mrr))
                paymentData.MonthlyRecurringRevenue = mrr;
            // ... other payment fields
        }

        // ... other helper methods for risk calculation, etc.

        private async Task<CustomerResponse> MapToUnifiedResponse(Customer customer)
        {
            var response = new CustomerResponse
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                JobTitle = customer.JobTitle,
                Location = customer.Location,
                Country = customer.Country,
                ChurnRiskScore = customer.ChurnRiskScore,
                ChurnRiskLevel = customer.ChurnRiskLevel,
                ChurnPredictionDate = customer.ChurnPredictionDate,
                DateCreated = customer.DateCreated,
                LastSyncedAt = customer.LastSyncedAt
            };

            // Get primary source data
            var primaryCrm = customer.GetPrimaryCrmData();
            if (primaryCrm != null)
            {
                response.PrimaryCrmInfo = new CustomerCrmInfo
                {
                    Source = primaryCrm.Source,
                    IsPrimarySource = true,
                    ImportBatchId = primaryCrm.ImportBatchId,
                    LeadSource = primaryCrm.LeadSource,
                    LifecycleStage = primaryCrm.LifecycleStage,
                    SalesOwnerName = primaryCrm.SalesOwnerName,
                    LastActivityDate = primaryCrm.LastActivityDate,
                    DealCount = primaryCrm.DealCount,
                    TotalDealValue = primaryCrm.TotalDealValue,
                    LastSyncedAt = primaryCrm.LastSyncedAt,
                    IsActive = primaryCrm.IsActive
                };
            }

            // Get all sources for advanced users
            response.AllCrmSources = await GetAllCrmSourcesAsync(customer.Id);
            response.AllPaymentSources = await GetAllPaymentSourcesAsync(customer.Id);

            return response;
        }

        private async Task RecalculateCustomerMetricsAsync(Customer customer)
        {
            customer.ChurnRiskScore = await CalculateAggregatedChurnRiskAsync(customer.Id);
            customer.ChurnRiskLevel = DetermineChurnRiskLevel(customer.ChurnRiskScore);
            customer.ChurnPredictionDate = DateTime.UtcNow;
        }

        private ChurnRiskLevel DetermineChurnRiskLevel(decimal riskScore)
        {
            return riskScore switch
            {
                >= 75 => ChurnRiskLevel.Critical,
                >= 50 => ChurnRiskLevel.High,
                >= 25 => ChurnRiskLevel.Medium,
                _ => ChurnRiskLevel.Low
            };
        }

        // Placeholder methods for risk calculation
        private decimal CalculatePaymentRisk(CustomerPaymentData paymentData) => 0.5m; // Implement actual logic
        private decimal CalculateEngagementRisk(CustomerEngagementData engagementData) => 0.3m; // Implement actual logic
    }
}
