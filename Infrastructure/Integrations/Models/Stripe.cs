using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Models
{
    public class StripeCustomer
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public bool Delinquent { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string DefaultSource { get; set; } = string.Empty;
        public int Balance { get; set; }
    }

    public class StripeSubscription
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime CurrentPeriodStart { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public DateTime? CanceledAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime? TrialStart { get; set; }
        public DateTime? TrialEnd { get; set; }
        public List<StripeSubscriptionItem> Items { get; set; } = new();
    }

    public class StripeSubscriptionItem
    {
        public string Id { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public StripePrice Price { get; set; } = new();
    }

    public class StripePrice
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public long UnitAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Recurring_Interval { get; set; } = string.Empty;
    }

    public class StripeInvoice
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long AmountDue { get; set; }
        public long AmountPaid { get; set; }
        public DateTime Created { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public int AttemptCount { get; set; }
    }
}
