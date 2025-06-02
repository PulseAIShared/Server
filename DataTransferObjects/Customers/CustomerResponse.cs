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
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }

        // Subscription details
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal LifetimeValue { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        // Engagement metrics
        public DateTime? LastLoginDate { get; set; }
        public int WeeklyLoginFrequency { get; set; }
        public decimal FeatureUsagePercentage { get; set; }
        public int SupportTicketCount { get; set; }

        // Churn prediction
        public decimal ChurnRiskScore { get; set; }
        public ChurnRiskLevel ChurnRiskLevel { get; set; }
        public DateTime? ChurnPredictionDate { get; set; }

        // Payment information
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int PaymentFailureCount { get; set; }

        // Demographics
        public string? Location { get; set; }
        public string? Country { get; set; }

        // Metadata
        public string Source { get; set; } = string.Empty;
        public DateTime LastSyncedAt { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
