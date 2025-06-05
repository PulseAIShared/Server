using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerMarketingInfo
    {
        public string Source { get; set; } = string.Empty;
        public bool IsPrimarySource { get; set; }
        public string? ImportBatchId { get; set; }
        public string? ImportedByUserName { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime? SubscriptionDate { get; set; }
        public DateTime? UnsubscriptionDate { get; set; }
        public double AverageOpenRate { get; set; }
        public double AverageClickRate { get; set; }
        public int TotalEmailsSent { get; set; }
        public int TotalEmailsOpened { get; set; }
        public int TotalEmailsClicked { get; set; }
        public DateTime? LastEmailOpenDate { get; set; }
        public DateTime? LastEmailClickDate { get; set; }
        public int CampaignCount { get; set; }
        public DateTime? LastCampaignEngagement { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> Lists { get; set; } = new();
        public DateTime LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
    }

}
