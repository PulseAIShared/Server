using Domain.Customers;
using Domain.Users;
using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Campaigns
{
    public class Campaign : Entity
    {
        [Required]
        public Guid CompanyId { get; set; } = Guid.Empty;

        public Guid? SegmentId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public CampaignType Type { get; set; }

        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

        public CampaignTrigger Trigger { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public DateTime? SentDate { get; set; }

        // Performance metrics
        public int SentCount { get; set; }

        public int OpenedCount { get; set; }

        public int ClickedCount { get; set; }

        public int ConvertedCount { get; set; }

        public decimal RevenueRecovered { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public CustomerSegment? Segment { get; set; }
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<CampaignStep> Steps { get; set; } = new List<CampaignStep>();
    }
}
