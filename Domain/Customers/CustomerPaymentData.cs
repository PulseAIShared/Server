using SharedKernel.Enums;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Customers
{
    public sealed class CustomerPaymentData : Entity
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public string Source { get; set; } = string.Empty; // stripe, paypal, manual_import, etc.

        [Required]
        public string ExternalId { get; set; } = string.Empty;

        public bool IsPrimarySource { get; set; } = false;
        public int SourcePriority { get; set; } = 0;

        // Subscription details
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal LifetimeValue { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public DateTime? TrialStartDate { get; set; }
        public DateTime? TrialEndDate { get; set; }

        // Payment information
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int PaymentFailureCount { get; set; }
        public DateTime? LastPaymentFailureDate { get; set; }
        public string? PaymentMethodType { get; set; }

        // Billing details
        public decimal CurrentBalance { get; set; }
        public string? Currency { get; set; }
        public string? BillingInterval { get; set; }

        // Import/sync metadata
        public string? ImportBatchId { get; set; }
        public Guid? ImportedByUserId { get; set; }
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public Customer Customer { get; set; } = null!;
        public Domain.Users.User? ImportedBy { get; set; }
    }
}
