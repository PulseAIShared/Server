using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Models
{
    public class SalesforceContact
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public string LeadSource { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public class SalesforceAccount
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public decimal? AnnualRevenue { get; set; }
        public int? NumberOfEmployees { get; set; }
        public string BillingCountry { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }

    public class SalesforceOpportunity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CloseDate { get; set; }
        public decimal Probability { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}
