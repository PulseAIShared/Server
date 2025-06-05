using SharedKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Customers
{
    public sealed class CustomerEngagementData : Entity
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public string Source { get; set; } = string.Empty; // google_analytics, mixpanel, amplitude, app_internal, manual_import, etc.

        [Required]
        public string ExternalId { get; set; } = string.Empty;

        public bool IsPrimarySource { get; set; } = false;
        public int SourcePriority { get; set; } = 0;

        // Login/Session data
        public DateTime? LastLoginDate { get; set; }
        public DateTime? FirstLoginDate { get; set; }
        public int WeeklyLoginFrequency { get; set; }
        public int MonthlyLoginFrequency { get; set; }
        public int TotalSessions { get; set; }
        public double AverageSessionDuration { get; set; } // in minutes

        // Feature usage
        public decimal FeatureUsagePercentage { get; set; }
        public Dictionary<string, object>? FeatureUsageCounts { get; set; }
        public DateTime? LastFeatureUsage { get; set; }

        // Behavioral metrics
        public int PageViews { get; set; }
        public double BounceRate { get; set; }
        public List<string> MostVisitedPages { get; set; } = new();

        // Custom events
        public Dictionary<string, object>? CustomEvents { get; set; }

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
