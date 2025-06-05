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

                case "marketing":
                    if (customer.MarketingDataSources.Any(m => m.Source == sourceName && m.IsActive))
                    {
                        foreach (var marketingData in customer.MarketingDataSources)
                            marketingData.IsPrimarySource = false;

                        var primaryMarketing = customer.MarketingDataSources.First(m => m.Source == sourceName);
                        primaryMarketing.IsPrimarySource = true;
                        customer.PrimaryMarketingSource = sourceName;
                    }
                    break;

                case "support":
                    if (customer.SupportDataSources.Any(s => s.Source == sourceName && s.IsActive))
                    {
                        foreach (var supportData in customer.SupportDataSources)
                            supportData.IsPrimarySource = false;

                        var primarySupport = customer.SupportDataSources.First(s => s.Source == sourceName);
                        primarySupport.IsPrimarySource = true;
                        customer.PrimarySupportSource = sourceName;
                    }
                    break;

                case "engagement":
                    if (customer.EngagementDataSources.Any(e => e.Source == sourceName && e.IsActive))
                    {
                        foreach (var engagementData in customer.EngagementDataSources)
                            engagementData.IsPrimarySource = false;

                        var primaryEngagement = customer.EngagementDataSources.First(e => e.Source == sourceName);
                        primaryEngagement.IsPrimarySource = true;
                        customer.PrimaryEngagementSource = sourceName;
                    }
                    break;
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

                    // Similar logic for other data types...
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

            // Support risk calculation (20% weight)
            foreach (var supportData in customer.SupportDataSources)
            {
                var weight = _sourcePriorities.GetValueOrDefault(supportData.Source, 10) / 100m;
                var risk = CalculateSupportRisk(supportData);
                totalRisk += risk * weight * 0.2m;
                totalWeight += weight * 0.2m;
            }

            // Marketing risk calculation (10% weight)
            foreach (var marketingData in customer.MarketingDataSources)
            {
                var weight = _sourcePriorities.GetValueOrDefault(marketingData.Source, 10) / 100m;
                var risk = CalculateMarketingRisk(marketingData);
                totalRisk += risk * weight * 0.1m;
                totalWeight += weight * 0.1m;
            }

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
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ??
                            sourceData.GetValueOrDefault("external_id")?.ToString() ?? "";

            // Find existing CRM data for this source
            var existingCrmData = customer.CrmDataSources
                .FirstOrDefault(c => c.Source == sourceName &&
                    (c.ExternalId == externalId || string.IsNullOrEmpty(externalId)));

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
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ??
                            sourceData.GetValueOrDefault("external_id")?.ToString() ?? "";

            var existingPaymentData = customer.PaymentDataSources
                .FirstOrDefault(p => p.Source == sourceName &&
                    (p.ExternalId == externalId || string.IsNullOrEmpty(externalId)));

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

        private async Task AddOrUpdateMarketingDataAsync(
            Customer customer,
            Dictionary<string, object> sourceData,
            string sourceName,
            string? importBatchId,
            Guid? importedByUserId)
        {
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ??
                            sourceData.GetValueOrDefault("external_id")?.ToString() ?? "";

            var existingMarketingData = customer.MarketingDataSources
                .FirstOrDefault(m => m.Source == sourceName);

            if (existingMarketingData != null)
            {
                UpdateMarketingDataFields(existingMarketingData, sourceData);
                existingMarketingData.LastSyncedAt = DateTime.UtcNow;
            }
            else
            {
                var newMarketingData = new CustomerMarketingData
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

                UpdateMarketingDataFields(newMarketingData, sourceData);

                var currentPrimary = customer.MarketingDataSources.FirstOrDefault(m => m.IsPrimarySource && m.IsActive);
                if (currentPrimary == null || newMarketingData.SourcePriority > currentPrimary.SourcePriority)
                {
                    if (currentPrimary != null)
                        currentPrimary.IsPrimarySource = false;

                    newMarketingData.IsPrimarySource = true;
                    customer.PrimaryMarketingSource = sourceName;
                }

                customer.MarketingDataSources.Add(newMarketingData);
                _context.CustomerMarketingData.Add(newMarketingData);
            }
        }

        private async Task AddOrUpdateSupportDataAsync(
            Customer customer,
            Dictionary<string, object> sourceData,
            string sourceName,
            string? importBatchId,
            Guid? importedByUserId)
        {
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ??
                            sourceData.GetValueOrDefault("external_id")?.ToString() ?? "";

            var existingSupportData = customer.SupportDataSources
                .FirstOrDefault(s => s.Source == sourceName);

            if (existingSupportData != null)
            {
                UpdateSupportDataFields(existingSupportData, sourceData);
                existingSupportData.LastSyncedAt = DateTime.UtcNow;
            }
            else
            {
                var newSupportData = new CustomerSupportData
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

                UpdateSupportDataFields(newSupportData, sourceData);

                var currentPrimary = customer.SupportDataSources.FirstOrDefault(s => s.IsPrimarySource && s.IsActive);
                if (currentPrimary == null || newSupportData.SourcePriority > currentPrimary.SourcePriority)
                {
                    if (currentPrimary != null)
                        currentPrimary.IsPrimarySource = false;

                    newSupportData.IsPrimarySource = true;
                    customer.PrimarySupportSource = sourceName;
                }

                customer.SupportDataSources.Add(newSupportData);
                _context.CustomerSupportData.Add(newSupportData);
            }
        }

        private async Task AddOrUpdateEngagementDataAsync(
            Customer customer,
            Dictionary<string, object> sourceData,
            string sourceName,
            string? importBatchId,
            Guid? importedByUserId)
        {
            var externalId = sourceData.GetValueOrDefault("id")?.ToString() ??
                            sourceData.GetValueOrDefault("external_id")?.ToString() ?? "";

            var existingEngagementData = customer.EngagementDataSources
                .FirstOrDefault(e => e.Source == sourceName);

            if (existingEngagementData != null)
            {
                UpdateEngagementDataFields(existingEngagementData, sourceData);
                existingEngagementData.LastSyncedAt = DateTime.UtcNow;
            }
            else
            {
                var newEngagementData = new CustomerEngagementData
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

                UpdateEngagementDataFields(newEngagementData, sourceData);

                var currentPrimary = customer.EngagementDataSources.FirstOrDefault(e => e.IsPrimarySource && e.IsActive);
                if (currentPrimary == null || newEngagementData.SourcePriority > currentPrimary.SourcePriority)
                {
                    if (currentPrimary != null)
                        currentPrimary.IsPrimarySource = false;

                    newEngagementData.IsPrimarySource = true;
                    customer.PrimaryEngagementSource = sourceName;
                }

                customer.EngagementDataSources.Add(newEngagementData);
                _context.CustomerEngagementData.Add(newEngagementData);
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

            if (ShouldUpdateField(customer.FirstName, sourceData.GetValueOrDefault("first_name")?.ToString() ??
                                 sourceData.GetValueOrDefault("firstname")?.ToString(), sourcePriority))
                customer.FirstName = sourceData.GetValueOrDefault("first_name")?.ToString() ??
                                   sourceData.GetValueOrDefault("firstname")?.ToString() ?? customer.FirstName;

            if (ShouldUpdateField(customer.LastName, sourceData.GetValueOrDefault("last_name")?.ToString() ??
                                 sourceData.GetValueOrDefault("lastname")?.ToString(), sourcePriority))
                customer.LastName = sourceData.GetValueOrDefault("last_name")?.ToString() ??
                                  sourceData.GetValueOrDefault("lastname")?.ToString() ?? customer.LastName;

            if (ShouldUpdateField(customer.Phone, sourceData.GetValueOrDefault("phone")?.ToString(), sourcePriority))
                customer.Phone = sourceData.GetValueOrDefault("phone")?.ToString();

            if (ShouldUpdateField(customer.CompanyName, sourceData.GetValueOrDefault("company_name")?.ToString() ??
                                 sourceData.GetValueOrDefault("company")?.ToString(), sourcePriority))
                customer.CompanyName = sourceData.GetValueOrDefault("company_name")?.ToString() ??
                                     sourceData.GetValueOrDefault("company")?.ToString();

            if (ShouldUpdateField(customer.JobTitle, sourceData.GetValueOrDefault("job_title")?.ToString() ??
                                 sourceData.GetValueOrDefault("jobtitle")?.ToString(), sourcePriority))
                customer.JobTitle = sourceData.GetValueOrDefault("job_title")?.ToString() ??
                                  sourceData.GetValueOrDefault("jobtitle")?.ToString();

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

        // Helper methods for updating specific data types
        private void UpdateCrmDataFields(CustomerCrmData crmData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("lead_source"))
                crmData.LeadSource = sourceData["lead_source"]?.ToString();
            if (sourceData.ContainsKey("lifecycle_stage") || sourceData.ContainsKey("lifecyclestage"))
                crmData.LifecycleStage = sourceData["lifecycle_stage"]?.ToString() ?? sourceData["lifecyclestage"]?.ToString();
            if (sourceData.ContainsKey("sales_owner_name"))
                crmData.SalesOwnerName = sourceData["sales_owner_name"]?.ToString();
            if (sourceData.ContainsKey("deal_count") && int.TryParse(sourceData["deal_count"]?.ToString(), out var dealCount))
                crmData.DealCount = dealCount;
            if (sourceData.ContainsKey("total_deal_value") && decimal.TryParse(sourceData["total_deal_value"]?.ToString(), out var totalValue))
                crmData.TotalDealValue = totalValue;
            if (sourceData.ContainsKey("last_activity_date") && DateTime.TryParse(sourceData["last_activity_date"]?.ToString(), out var lastActivity))
                crmData.LastActivityDate = lastActivity;
            if (sourceData.ContainsKey("first_contact_date") && DateTime.TryParse(sourceData["first_contact_date"]?.ToString(), out var firstContact))
                crmData.FirstContactDate = firstContact;
        }

        private void UpdatePaymentDataFields(CustomerPaymentData paymentData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("subscription_status") && Enum.TryParse<SubscriptionStatus>(sourceData["subscription_status"]?.ToString(), true, out var subStatus))
                paymentData.SubscriptionStatus = subStatus;
            if (sourceData.ContainsKey("plan") && Enum.TryParse<SubscriptionPlan>(sourceData["plan"]?.ToString(), true, out var plan))
                paymentData.Plan = plan;
            if (sourceData.ContainsKey("mrr") && decimal.TryParse(sourceData["mrr"]?.ToString(), out var mrr))
                paymentData.MonthlyRecurringRevenue = mrr;
            if (sourceData.ContainsKey("monthly_revenue") && decimal.TryParse(sourceData["monthly_revenue"]?.ToString(), out var monthlyRevenue))
                paymentData.MonthlyRecurringRevenue = monthlyRevenue;
            if (sourceData.ContainsKey("lifetime_value") && decimal.TryParse(sourceData["lifetime_value"]?.ToString(), out var ltv))
                paymentData.LifetimeValue = ltv;
            if (sourceData.ContainsKey("subscription_start_date") && DateTime.TryParse(sourceData["subscription_start_date"]?.ToString(), out var subStartDate))
                paymentData.SubscriptionStartDate = subStartDate;
            if (sourceData.ContainsKey("payment_status") && Enum.TryParse<PaymentStatus>(sourceData["payment_status"]?.ToString(), true, out var payStatus))
                paymentData.PaymentStatus = payStatus;
            if (sourceData.ContainsKey("next_billing_date") && DateTime.TryParse(sourceData["next_billing_date"]?.ToString(), out var nextBilling))
                paymentData.NextBillingDate = nextBilling;
            if (sourceData.ContainsKey("payment_failures") && int.TryParse(sourceData["payment_failures"]?.ToString(), out var failures))
                paymentData.PaymentFailureCount = failures;
        }

        private void UpdateMarketingDataFields(CustomerMarketingData marketingData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("is_subscribed") && bool.TryParse(sourceData["is_subscribed"]?.ToString(), out var isSubscribed))
                marketingData.IsSubscribed = isSubscribed;
            if (sourceData.ContainsKey("open_rate") && double.TryParse(sourceData["open_rate"]?.ToString(), out var openRate))
                marketingData.AverageOpenRate = openRate;
            if (sourceData.ContainsKey("click_rate") && double.TryParse(sourceData["click_rate"]?.ToString(), out var clickRate))
                marketingData.AverageClickRate = clickRate;
            if (sourceData.ContainsKey("emails_sent") && int.TryParse(sourceData["emails_sent"]?.ToString(), out var emailsSent))
                marketingData.TotalEmailsSent = emailsSent;
            if (sourceData.ContainsKey("campaign_count") && int.TryParse(sourceData["campaign_count"]?.ToString(), out var campaignCount))
                marketingData.CampaignCount = campaignCount;
        }

        private void UpdateSupportDataFields(CustomerSupportData supportData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("total_tickets") && int.TryParse(sourceData["total_tickets"]?.ToString(), out var totalTickets))
                supportData.TotalTickets = totalTickets;
            if (sourceData.ContainsKey("open_tickets") && int.TryParse(sourceData["open_tickets"]?.ToString(), out var openTickets))
                supportData.OpenTickets = openTickets;
            if (sourceData.ContainsKey("csat_score") && double.TryParse(sourceData["csat_score"]?.ToString(), out var csatScore))
                supportData.CustomerSatisfactionScore = csatScore;
            if (sourceData.ContainsKey("avg_resolution_time") && double.TryParse(sourceData["avg_resolution_time"]?.ToString(), out var avgResTime))
                supportData.AverageResolutionTime = avgResTime;
        }

        private void UpdateEngagementDataFields(CustomerEngagementData engagementData, Dictionary<string, object> sourceData)
        {
            if (sourceData.ContainsKey("last_login") && DateTime.TryParse(sourceData["last_login"]?.ToString(), out var lastLogin))
                engagementData.LastLoginDate = lastLogin;
            if (sourceData.ContainsKey("weekly_logins") && int.TryParse(sourceData["weekly_logins"]?.ToString(), out var weeklyLogins))
                engagementData.WeeklyLoginFrequency = weeklyLogins;
            if (sourceData.ContainsKey("feature_usage") && decimal.TryParse(sourceData["feature_usage"]?.ToString(), out var featureUsage))
                engagementData.FeatureUsagePercentage = featureUsage;
            if (sourceData.ContainsKey("session_count") && int.TryParse(sourceData["session_count"]?.ToString(), out var sessionCount))
                engagementData.TotalSessions = sessionCount;
            if (sourceData.ContainsKey("avg_session_duration") && double.TryParse(sourceData["avg_session_duration"]?.ToString(), out var avgSessionDuration))
                engagementData.AverageSessionDuration = avgSessionDuration;
        }

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

            // Get all sources for comprehensive view
            response.CrmInfo = await GetAllCrmSourcesAsync(customer.Id);
            response.PaymentInfo = await GetAllPaymentSourcesAsync(customer.Id);
            // Add other source types as needed

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

        // Risk calculation methods
        private decimal CalculatePaymentRisk(CustomerPaymentData paymentData)
        {
            decimal risk = 0;

            // Payment status risk
            risk += paymentData.PaymentStatus switch
            {
                PaymentStatus.Failed => 0.8m,
                PaymentStatus.PastDue => 0.6m,
                PaymentStatus.Cancelled => 1.0m,
                _ => 0.1m
            };

            // Subscription status risk
            risk += paymentData.SubscriptionStatus switch
            {
                SubscriptionStatus.Cancelled => 1.0m,
                SubscriptionStatus.Expired => 0.9m,
                SubscriptionStatus.PastDue => 0.7m,
                SubscriptionStatus.Trial => 0.3m,
                _ => 0.1m
            };

            // Payment failure count
            if (paymentData.PaymentFailureCount > 0)
                risk += Math.Min(0.3m, paymentData.PaymentFailureCount * 0.1m);

            // Recent payment failure
            if (paymentData.LastPaymentFailureDate.HasValue &&
                paymentData.LastPaymentFailureDate.Value > DateTime.UtcNow.AddDays(-30))
                risk += 0.2m;

            return Math.Min(1.0m, risk);
        }

        private decimal CalculateEngagementRisk(CustomerEngagementData engagementData)
        {
            decimal risk = 0;

            // Last login risk
            if (engagementData.LastLoginDate.HasValue)
            {
                var daysSinceLogin = (DateTime.UtcNow - engagementData.LastLoginDate.Value).TotalDays;
                risk += daysSinceLogin switch
                {
                    > 30 => 0.8m,
                    > 14 => 0.5m,
                    > 7 => 0.3m,
                    _ => 0.1m
                };
            }
            else
            {
                risk += 0.9m; // No login data is high risk
            }

            // Login frequency risk
            risk += engagementData.WeeklyLoginFrequency switch
            {
                0 => 0.8m,
                1 => 0.4m,
                2 => 0.2m,
                _ => 0.0m
            };

            // Feature usage risk
            risk += engagementData.FeatureUsagePercentage switch
            {
                < 10 => 0.7m,
                < 25 => 0.4m,
                < 50 => 0.2m,
                _ => 0.0m
            };

            return Math.Min(1.0m, risk / 3); // Average the risk factors
        }

        private decimal CalculateSupportRisk(CustomerSupportData supportData)
        {
            decimal risk = 0;

            // High number of open tickets
            if (supportData.OpenTickets > 3)
                risk += 0.5m;
            else if (supportData.OpenTickets > 1)
                risk += 0.2m;

            // Low satisfaction score
            if (supportData.CustomerSatisfactionScore < 3.0)
                risk += 0.6m;
            else if (supportData.CustomerSatisfactionScore < 4.0)
                risk += 0.3m;

            // High number of urgent tickets
            if (supportData.UrgentTickets > 0)
                risk += 0.4m;

            return Math.Min(1.0m, risk);
        }

        private decimal CalculateMarketingRisk(CustomerMarketingData marketingData)
        {
            decimal risk = 0;

            // Not subscribed to marketing
            if (!marketingData.IsSubscribed)
                risk += 0.4m;

            // Low engagement rates
            if (marketingData.AverageOpenRate < 0.1) // Less than 10%
                risk += 0.3m;

            if (marketingData.AverageClickRate < 0.02) // Less than 2%
                risk += 0.2m;

            // No recent engagement
            if (marketingData.LastCampaignEngagement.HasValue &&
                marketingData.LastCampaignEngagement.Value < DateTime.UtcNow.AddDays(-60))
                risk += 0.3m;

            return Math.Min(1.0m, risk);
        }
    }
}