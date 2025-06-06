using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public  class CustomerResponse
    {
        // Core customer info (always available)
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }

        // Aggregated churn info (calculated from all sources)
        public decimal ChurnRiskScore { get; set; }
        public ChurnRiskLevel ChurnRiskLevel { get; set; }
        public DateTime? ChurnPredictionDate { get; set; }

        // Source-specific data collections (can be null/empty if no sources)
        public List<CustomerCrmInfo>? CrmInfo { get; set; }
        public List<CustomerPaymentInfo>? PaymentInfo { get; set; }
        public List<CustomerMarketingInfo>? MarketingInfo { get; set; }
        public List<CustomerSupportInfo>? SupportInfo { get; set; }
        public List<CustomerEngagementInfo>? EngagementInfo { get; set; }

        // Primary source indicators (for quick access to main data)
        public string? PrimaryCrmSource { get; set; }
        public string? PrimaryPaymentSource { get; set; }
        public string? PrimaryMarketingSource { get; set; }
        public string? PrimarySupportSource { get; set; }
        public string? PrimaryEngagementSource { get; set; }

        // Quick access to primary source data (derived from collections above)
        public CustomerCrmInfo? PrimaryCrmInfo => CrmInfo?.FirstOrDefault(c => c.IsPrimarySource);
        public CustomerPaymentInfo? PrimaryPaymentInfo => PaymentInfo?.FirstOrDefault(p => p.IsPrimarySource);
        public CustomerMarketingInfo? PrimaryMarketingInfo => MarketingInfo?.FirstOrDefault(m => m.IsPrimarySource);
        public CustomerSupportInfo? PrimarySupportInfo => SupportInfo?.FirstOrDefault(s => s.IsPrimarySource);
        public CustomerEngagementInfo? PrimaryEngagementInfo => EngagementInfo?.FirstOrDefault(e => e.IsPrimarySource);

        // Legacy computed properties for backwards compatibility (uses primary sources)
        public SubscriptionStatus SubscriptionStatus => PrimaryPaymentInfo?.SubscriptionStatus ?? SubscriptionStatus.Trial;
        public SubscriptionPlan Plan => PrimaryPaymentInfo?.Plan ?? SubscriptionPlan.Basic;
        public decimal MonthlyRecurringRevenue => PrimaryPaymentInfo?.MonthlyRecurringRevenue ?? 0;
        public decimal LifetimeValue => PrimaryPaymentInfo?.LifetimeValue ?? 0;
        public DateTime? SubscriptionStartDate => PrimaryPaymentInfo?.SubscriptionStartDate;
        public DateTime? SubscriptionEndDate => PrimaryPaymentInfo?.SubscriptionEndDate;
        public DateTime? LastLoginDate => PrimaryEngagementInfo?.LastLoginDate;
        public int WeeklyLoginFrequency => PrimaryEngagementInfo?.WeeklyLoginFrequency ?? 0;
        public decimal FeatureUsagePercentage => PrimaryEngagementInfo?.FeatureUsagePercentage ?? 0;
        public int SupportTicketCount => PrimarySupportInfo?.TotalTickets ?? 0;
        public PaymentStatus PaymentStatus => PrimaryPaymentInfo?.PaymentStatus ?? PaymentStatus.Active;
        public DateTime? LastPaymentDate => PrimaryPaymentInfo?.LastPaymentDate;
        public DateTime? NextBillingDate => PrimaryPaymentInfo?.NextBillingDate;
        public int PaymentFailureCount => PrimaryPaymentInfo?.PaymentFailureCount ?? 0;

        // Metadata
        public DateTime DateCreated { get; set; }
        public DateTime LastSyncedAt { get; set; }

        // Source summary for quick overview
        public CustomerSourceSummary SourceSummary => new()
        {
            CrmSourceCount = CrmInfo?.Count ?? 0,
            PaymentSourceCount = PaymentInfo?.Count ?? 0,
            MarketingSourceCount = MarketingInfo?.Count ?? 0,
            SupportSourceCount = SupportInfo?.Count ?? 0,
            EngagementSourceCount = EngagementInfo?.Count ?? 0,
            TotalSources = (CrmInfo?.Count ?? 0) + (PaymentInfo?.Count ?? 0) +
                          (MarketingInfo?.Count ?? 0) + (SupportInfo?.Count ?? 0) +
                          (EngagementInfo?.Count ?? 0)
        };
    }

    public sealed class CustomerSourceSummary
    {
        public int CrmSourceCount { get; set; }
        public int PaymentSourceCount { get; set; }
        public int MarketingSourceCount { get; set; }
        public int SupportSourceCount { get; set; }
        public int EngagementSourceCount { get; set; }
        public int TotalSources { get; set; }

        public bool HasMultipleSources => TotalSources > 1;
        public bool HasPaymentData => PaymentSourceCount > 0;
        public bool HasCrmData => CrmSourceCount > 0;
        public bool HasMarketingData => MarketingSourceCount > 0;
        public bool HasSupportData => SupportSourceCount > 0;
        public bool HasEngagementData => EngagementSourceCount > 0;
    }


}