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
    public class CampaignStep : Entity
    {
        [Required]
        public Guid CampaignId { get; set; } = Guid.Empty;

        public int StepOrder { get; set; }

        public CampaignType Type { get; set; }

        public TimeSpan Delay { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Performance metrics for this step
        public int SentCount { get; set; }

        public int OpenedCount { get; set; }

        public int ClickedCount { get; set; }

        public int ConvertedCount { get; set; }

        // Navigation properties
        public Campaign Campaign { get; set; } = null!;
    }
}
