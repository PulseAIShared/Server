using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Services;
using DataTransferObjects.Customers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Queries
{
    internal sealed class GetCustomerByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICustomerAggregationService customerAggregationService,
        ILogger<GetCustomerByIdQueryHandler> logger)
        : IQueryHandler<GetCustomerByIdQuery, CustomerDetailResponse>
    {
        public async Task<Result<CustomerDetailResponse>> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<CustomerDetailResponse>(UserErrors.NotFound(userContext.UserId));
            }

            // Get customer with ALL data sources included
            var customer = await context.Customers
                .Include(c => c.Activities.OrderByDescending(a => a.ActivityDate).Take(20)) // Get more activities
                .Include(c => c.ChurnPredictions.OrderByDescending(cp => cp.PredictionDate).Take(10)) // Get more predictions
                                                                                                      // Include ALL data sources for comprehensive view
                .Include(c => c.CrmDataSources.Where(cds => cds.IsActive))
                    .ThenInclude(cds => cds.ImportedBy)
                .Include(c => c.PaymentDataSources.Where(pds => pds.IsActive))
                    .ThenInclude(pds => pds.ImportedBy)
                .Include(c => c.MarketingDataSources.Where(mds => mds.IsActive))
                    .ThenInclude(mds => mds.ImportedBy)
                .Include(c => c.SupportDataSources.Where(sds => sds.IsActive))
                    .ThenInclude(sds => sds.ImportedBy)
                .Include(c => c.EngagementDataSources.Where(eds => eds.IsActive))
                    .ThenInclude(eds => eds.ImportedBy)
                .FirstOrDefaultAsync(c => c.Id == query.CustomerId && c.CompanyId == user.CompanyId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CustomerDetailResponse>(Error.NotFound(
                    "Customer.NotFound",
                    $"Customer with ID {query.CustomerId} was not found"));
            }

                // Use aggregation service to get unified data with all sources
                var unifiedCustomer = await customerAggregationService.GetUnifiedCustomerAsync(customer.Id, user.CompanyId);

                // Create detailed response with unified data + additional detail fields
                var response = new CustomerDetailResponse
                {
                    // Copy all base fields from unified response
                    Id = unifiedCustomer.Id,
                    FirstName = unifiedCustomer.FirstName,
                    LastName = unifiedCustomer.LastName,
                    Email = unifiedCustomer.Email,
                    Phone = unifiedCustomer.Phone,
                    CompanyName = unifiedCustomer.CompanyName,
                    JobTitle = unifiedCustomer.JobTitle,
                    Location = unifiedCustomer.Location,
                    Country = unifiedCustomer.Country,
                    ChurnRiskScore = unifiedCustomer.ChurnRiskScore,
                    ChurnRiskLevel = unifiedCustomer.ChurnRiskLevel,
                    ChurnPredictionDate = unifiedCustomer.ChurnPredictionDate,
                    DateCreated = unifiedCustomer.DateCreated,
                    LastSyncedAt = unifiedCustomer.LastSyncedAt,

                    // Copy all source data collections
                    CrmInfo = unifiedCustomer.CrmInfo,
                    PaymentInfo = unifiedCustomer.PaymentInfo,
                    MarketingInfo = unifiedCustomer.MarketingInfo,
                    SupportInfo = unifiedCustomer.SupportInfo,
                    EngagementInfo = unifiedCustomer.EngagementInfo,

                    // Copy primary source indicators
                    PrimaryCrmSource = unifiedCustomer.PrimaryCrmSource,
                    PrimaryPaymentSource = unifiedCustomer.PrimaryPaymentSource,
                    PrimaryMarketingSource = unifiedCustomer.PrimaryMarketingSource,
                    PrimarySupportSource = unifiedCustomer.PrimarySupportSource,
                    PrimaryEngagementSource = unifiedCustomer.PrimaryEngagementSource,

                    // Additional detail fields specific to detail view
                    Age = customer.Age,
                    Gender = customer.Gender,
                    TimeZone = customer.TimeZone,

                    // Recent activities with enhanced data
                    RecentActivities = customer.Activities.Select(a => new CustomerActivityResponse
                    {
                        Id = a.Id,
                        Type = a.Type,
                        Description = a.Description,
                        Metadata = a.Metadata,
                        ActivityDate = a.ActivityDate
                    }).ToList(),

                    // Churn prediction history
                    ChurnHistory = customer.ChurnPredictions.Select(cp => new ChurnPredictionResponse
                    {
                        Id = cp.Id,
                        RiskScore = cp.RiskScore,
                        RiskLevel = cp.RiskLevel,
                        PredictionDate = cp.PredictionDate,
                        RiskFactors = cp.RiskFactors,
                        ModelVersion = cp.ModelVersion
                    }).ToList(),

                    // Add data source details for detailed view
                    DataSourceDetails = new CustomerDataSourceDetails
                    {
                        CrmSources = customer.CrmDataSources.Select(c => new DataSourceInfo
                        {
                            Source = c.Source,
                            ExternalId = c.ExternalId,
                            IsPrimarySource = c.IsPrimarySource,
                            LastSyncedAt = c.LastSyncedAt,
                            ImportBatchId = c.ImportBatchId,
                            ImportedByUserName = c.ImportedBy != null ? $"{c.ImportedBy.FirstName} {c.ImportedBy.LastName}" : null,
                            SourcePriority = c.SourcePriority,
                            IsActive = c.IsActive
                        }).ToList(),

                        PaymentSources = customer.PaymentDataSources.Select(p => new DataSourceInfo
                        {
                            Source = p.Source,
                            ExternalId = p.ExternalId,
                            IsPrimarySource = p.IsPrimarySource,
                            LastSyncedAt = p.LastSyncedAt,
                            ImportBatchId = p.ImportBatchId,
                            ImportedByUserName = p.ImportedBy != null ? $"{p.ImportedBy.FirstName} {p.ImportedBy.LastName}" : null,
                            SourcePriority = p.SourcePriority,
                            IsActive = p.IsActive
                        }).ToList(),

                        MarketingSources = customer.MarketingDataSources.Select(m => new DataSourceInfo
                        {
                            Source = m.Source,
                            ExternalId = m.ExternalId,
                            IsPrimarySource = m.IsPrimarySource,
                            LastSyncedAt = m.LastSyncedAt,
                            ImportBatchId = m.ImportBatchId,
                            ImportedByUserName = m.ImportedBy != null ? $"{m.ImportedBy.FirstName} {m.ImportedBy.LastName}" : null,
                            SourcePriority = m.SourcePriority,
                            IsActive = m.IsActive
                        }).ToList(),

                        SupportSources = customer.SupportDataSources.Select(s => new DataSourceInfo
                        {
                            Source = s.Source,
                            ExternalId = s.ExternalId,
                            IsPrimarySource = s.IsPrimarySource,
                            LastSyncedAt = s.LastSyncedAt,
                            ImportBatchId = s.ImportBatchId,
                            ImportedByUserName = s.ImportedBy != null ? $"{s.ImportedBy.FirstName} {s.ImportedBy.LastName}" : null,
                            SourcePriority = s.SourcePriority,
                            IsActive = s.IsActive
                        }).ToList(),

                        EngagementSources = customer.EngagementDataSources.Select(e => new DataSourceInfo
                        {
                            Source = e.Source,
                            ExternalId = e.ExternalId,
                            IsPrimarySource = e.IsPrimarySource,
                            LastSyncedAt = e.LastSyncedAt,
                            ImportBatchId = e.ImportBatchId,
                            ImportedByUserName = e.ImportedBy != null ? $"{e.ImportedBy.FirstName} {e.ImportedBy.LastName}" : null,
                            SourcePriority = e.SourcePriority,
                            IsActive = e.IsActive
                        }).ToList()
                    },

                    // Add quick metrics for the detail view
                    QuickMetrics = new CustomerQuickMetrics
                    {
                        TotalDataSources = customer.CrmDataSources.Count(c => c.IsActive) +
                                         customer.PaymentDataSources.Count(p => p.IsActive) +
                                         customer.MarketingDataSources.Count(m => m.IsActive) +
                                         customer.SupportDataSources.Count(s => s.IsActive) +
                                         customer.EngagementDataSources.Count(e => e.IsActive),

                        LastActivityDate = customer.Activities.Any() ?
                            customer.Activities.Max(a => a.ActivityDate) : (DateTime?)null,

                        LastDataSync = new[] {
                            customer.CrmDataSources.Where(c => c.IsActive).Max(c => (DateTime?)c.LastSyncedAt),
                            customer.PaymentDataSources.Where(p => p.IsActive).Max(p => (DateTime?)p.LastSyncedAt),
                            customer.MarketingDataSources.Where(m => m.IsActive).Max(m => (DateTime?)m.LastSyncedAt),
                            customer.SupportDataSources.Where(s => s.IsActive).Max(s => (DateTime?)s.LastSyncedAt),
                            customer.EngagementDataSources.Where(e => e.IsActive).Max(e => (DateTime?)e.LastSyncedAt)
                        }.Where(d => d.HasValue).DefaultIfEmpty().Max(),

                        TotalActivities = customer.Activities.Count,
                        TotalChurnPredictions = customer.ChurnPredictions.Count,

                        HasRecentActivity = customer.Activities.Any(a => a.ActivityDate > DateTime.UtcNow.AddDays(-30)),
                        HasMultipleSources = (customer.CrmDataSources.Count(c => c.IsActive) +
                                            customer.PaymentDataSources.Count(p => p.IsActive) +
                                            customer.MarketingDataSources.Count(m => m.IsActive) +
                                            customer.SupportDataSources.Count(s => s.IsActive) +
                                            customer.EngagementDataSources.Count(e => e.IsActive)) > 1,

                        DataCompletenessScore = CalculateDataCompletenessScore(customer)
                    }
                };

                logger.LogInformation("Successfully retrieved detailed customer data for {CustomerId} with {SourceCount} active data sources",
                    query.CustomerId, response.QuickMetrics.TotalDataSources);

                return response;
                
        }

        private decimal CalculateDataCompletenessScore(Domain.Customers.Customer customer)
        {
            var totalPossibleFields = 15; // Core fields we expect
            var completedFields = 0;

            // Core customer data (5 points)
            if (!string.IsNullOrEmpty(customer.FirstName)) completedFields++;
            if (!string.IsNullOrEmpty(customer.LastName)) completedFields++;
            if (!string.IsNullOrEmpty(customer.Email)) completedFields++;
            if (!string.IsNullOrEmpty(customer.Phone)) completedFields++;
            if (!string.IsNullOrEmpty(customer.CompanyName)) completedFields++;

            // Additional profile data (3 points)
            if (!string.IsNullOrEmpty(customer.JobTitle)) completedFields++;
            if (!string.IsNullOrEmpty(customer.Location)) completedFields++;
            if (!string.IsNullOrEmpty(customer.Country)) completedFields++;

            // Data source presence (5 points)
            if (customer.CrmDataSources.Any(c => c.IsActive)) completedFields++;
            if (customer.PaymentDataSources.Any(p => p.IsActive)) completedFields++;
            if (customer.MarketingDataSources.Any(m => m.IsActive)) completedFields++;
            if (customer.SupportDataSources.Any(s => s.IsActive)) completedFields++;
            if (customer.EngagementDataSources.Any(e => e.IsActive)) completedFields++;

            // Activity data (2 points)
            if (customer.Activities.Any()) completedFields++;
            if (customer.ChurnPredictions.Any()) completedFields++;

            return Math.Round((decimal)completedFields / totalPossibleFields * 100, 1);
        }

    }

}