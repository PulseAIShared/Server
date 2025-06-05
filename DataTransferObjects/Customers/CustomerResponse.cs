using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerResponse
    {
        // Core customer info
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }

        // Aggregated churn info
        public decimal ChurnRiskScore { get; set; }
        public ChurnRiskLevel ChurnRiskLevel { get; set; }
        public DateTime? ChurnPredictionDate { get; set; }

        // CRM data (if available)
        public List<CustomerCrmInfo>? CrmInfo { get; set; }

        // Payment data (if available)
        public List<CustomerPaymentInfo>? PaymentInfo { get; set; }

        // Marketing data (if available)
        public List<CustomerMarketingInfo>? MarketingInfo { get; set; }

        // Support data (if available)
        public List<CustomerSupportInfo>? SupportInfo { get; set; }

        // Engagement data (if available)
        public List<CustomerEngagementInfo>? EngagementInfo { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime LastSyncedAt { get; set; }
    }

}
