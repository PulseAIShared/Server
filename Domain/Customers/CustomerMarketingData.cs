using SharedKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Customers
{
    public sealed class CustomerMarketingData : Entity
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public string Source { get; set; } = string.Empty; // mailchimp, klaviyo, sendgrid, manual_import, etc.

        [Required]
        public string ExternalId { get; set; } = string.Empty;

        public bool IsPrimarySource { get; set; } = false;
        public int SourcePriority { get; set; } = 0;

        // Email marketing metrics
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

        // Campaign engagement
        public int CampaignCount { get; set; }
        public DateTime? LastCampaignEngagement { get; set; }

        // Segmentation
        public List<string> Tags { get; set; } = new();
        public List<string> Lists { get; set; } = new();

        // Import/sync metadata
        public string? ImportBatchId { get; set; }
        public Guid? ImportedByUserId { get; set; }
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
        public string? SyncVersion { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Customer Customer { get; set; } = null!;
        public Domain.Users.User? ImportedBy { get; set; }
    }

}
