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
    public class CustomerSegment : Entity
    {
        [Required]
        public Guid CompanyId { get; set; } = Guid.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public SegmentType Type { get; set; }

        public SegmentStatus Status { get; set; } = SegmentStatus.Active;

        public string Color { get; set; } = "#3b82f6";

        public int CustomerCount { get; set; }

        public decimal AverageChurnRate { get; set; }

        public decimal AverageLifetimeValue { get; set; }

        public decimal AverageRevenue { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public ICollection<SegmentCriteria> Criteria { get; set; } = new List<SegmentCriteria>();
        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    }
}
