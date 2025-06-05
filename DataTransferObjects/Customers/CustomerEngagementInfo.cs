using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerEngagementInfo
    {
        public string Source { get; set; } = string.Empty;
        public bool IsPrimarySource { get; set; }
        public string? ImportBatchId { get; set; }
        public string? ImportedByUserName { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? FirstLoginDate { get; set; }
        public int WeeklyLoginFrequency { get; set; }
        public int MonthlyLoginFrequency { get; set; }
        public int TotalSessions { get; set; }
        public double AverageSessionDuration { get; set; }
        public decimal FeatureUsagePercentage { get; set; }
        public Dictionary<string, object>? FeatureUsageCounts { get; set; }
        public DateTime? LastFeatureUsage { get; set; }
        public int PageViews { get; set; }
        public double BounceRate { get; set; }
        public List<string> MostVisitedPages { get; set; } = new();
        public Dictionary<string, object>? CustomEvents { get; set; }
        public DateTime LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
