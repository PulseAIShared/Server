using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerPaymentInfo
    {
        public string Source { get; set; } = string.Empty;
        public bool IsPrimarySource { get; set; }
        public string? ImportBatchId { get; set; }
        public string? ImportedByUserName { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal LifetimeValue { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int PaymentFailureCount { get; set; }

        public DateTime LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
    }

}
