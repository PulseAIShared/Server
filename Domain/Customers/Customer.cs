using Domain.Campaigns;
using Domain.Segments;
using Domain.Users;
using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Customers
{
    public class Customer : Entity
    {
        [Required]
        public Guid CompanyId { get; set; } = Guid.Empty;

        [Required]
        public string ExternalId { get; set; } = string.Empty;

        [Required]
        public string Source { get; set; } = string.Empty; // salesforce, hubspot, etc.

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
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

        // Demographics
        public int? Age { get; set; }

        public string? Gender { get; set; }

        public string? Location { get; set; }

        public string? Country { get; set; }

        public string? TimeZone { get; set; }

        // Payment information
        public PaymentStatus PaymentStatus { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        public DateTime? NextBillingDate { get; set; }

        public int PaymentFailureCount { get; set; }

        public DateTime? LastPaymentFailureDate { get; set; }

        // Sync metadata
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

        public string? SyncVersion { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public ICollection<CustomerActivity> Activities { get; set; } = new List<CustomerActivity>();
        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public ICollection<ChurnPrediction> ChurnPredictions { get; set; } = new List<ChurnPrediction>();
    }
}
