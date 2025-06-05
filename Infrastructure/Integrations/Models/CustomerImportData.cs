using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Models
{
    public class CustomerImportData
    {
        // Core customer information
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }
        public string? TimeZone { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }

        // Subscription/Payment data
        public SubscriptionStatus? SubscriptionStatus { get; set; }
        public SubscriptionPlan? Plan { get; set; }
        public decimal? MonthlyRecurringRevenue { get; set; }
        public decimal? LifetimeValue { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int? PaymentFailureCount { get; set; }

        // Engagement data
        public DateTime? LastLoginDate { get; set; }
        public int? WeeklyLoginFrequency { get; set; }
        public decimal? FeatureUsagePercentage { get; set; }
        public int? SupportTicketCount { get; set; }

        // Meta information
        public string ImportSource { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public int RowNumber { get; set; }
    }

    public class DuplicateAnalysisResult
    {
        public HashSet<string> ExistingEmails { get; set; } = new();
        public HashSet<string> SkippedEmails { get; set; } = new();
    }
}
