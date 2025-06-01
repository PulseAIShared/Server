using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Models
{
    public class HubSpotContact
    {
        public string Id { get; set; } = string.Empty;
        public HubSpotContactProperties Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class HubSpotContactProperties
    {
        public string Email { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Jobtitle { get; set; } = string.Empty;
        public string Lifecyclestage { get; set; } = string.Empty;
        public string Hubspot_owner_id { get; set; } = string.Empty;
        public DateTime? Lastmodifieddate { get; set; }
        public DateTime? Last_activity_date { get; set; }
        public string Hs_lead_status { get; set; } = string.Empty;
    }

    public class HubSpotCompany
    {
        public string Id { get; set; } = string.Empty;
        public HubSpotCompanyProperties Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class HubSpotCompanyProperties
    {
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int? Numberofemployees { get; set; }
        public decimal? Annualrevenue { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class HubSpotDeal
    {
        public string Id { get; set; } = string.Empty;
        public HubSpotDealProperties Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class HubSpotDealProperties
    {
        public string Dealname { get; set; } = string.Empty;
        public string Dealstage { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public DateTime? Closedate { get; set; }
        public string Pipeline { get; set; } = string.Empty;
        public string Dealtype { get; set; } = string.Empty;
    }
}
