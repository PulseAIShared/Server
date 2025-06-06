using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerDetailResponse : CustomerResponse
    {
        // Additional profile fields specific to detail view
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? TimeZone { get; set; }

        // Activity and prediction history
        public List<CustomerActivityResponse> RecentActivities { get; set; } = new();
        public List<ChurnPredictionResponse> ChurnHistory { get; set; } = new();

        // Detailed data source information for admin/detail views
        public CustomerDataSourceDetails DataSourceDetails { get; set; } = new();

        // Quick metrics and insights for the detail view
        public CustomerQuickMetrics QuickMetrics { get; set; } = new();

        // Data quality and completeness indicators
        public DataQualityInfo DataQuality => new()
        {
            CompletenessScore = QuickMetrics.DataCompletenessScore,
            HasMultipleSources = QuickMetrics.HasMultipleSources,
            LastDataSync = QuickMetrics.LastDataSync,
            MissingCriticalData = GetMissingCriticalData(),
            DataFreshness = CalculateDataFreshness(),
            RecommendedActions = GetRecommendedActions()
        };

        // Helper methods for data quality assessment
        private List<string> GetMissingCriticalData()
        {
            var missing = new List<string>();

            if (string.IsNullOrEmpty(Phone)) missing.Add("Phone Number");
            if (string.IsNullOrEmpty(CompanyName)) missing.Add("Company Name");
            if (string.IsNullOrEmpty(JobTitle)) missing.Add("Job Title");
            if (PaymentInfo?.Any() != true) missing.Add("Payment Information");
            if (EngagementInfo?.Any() != true) missing.Add("Engagement Data");
            if (QuickMetrics.LastActivityDate == null || QuickMetrics.LastActivityDate < DateTime.UtcNow.AddDays(-60))
                missing.Add("Recent Activity");

            return missing;
        }

        private string CalculateDataFreshness()
        {
            if (QuickMetrics.LastDataSync == null) return "Unknown";

            var daysSinceSync = (DateTime.UtcNow - QuickMetrics.LastDataSync.Value).TotalDays;

            return daysSinceSync switch
            {
                <= 1 => "Excellent",
                <= 7 => "Good",
                <= 30 => "Fair",
                _ => "Stale"
            };
        }

        private List<string> GetRecommendedActions()
        {
            var actions = new List<string>();

            if (ChurnRiskLevel >= SharedKernel.Enums.ChurnRiskLevel.High)
                actions.Add("Schedule immediate outreach");

            if (QuickMetrics.HasRecentActivity == false)
                actions.Add("Re-engagement campaign needed");

            if (PaymentInfo?.Any(p => p.PaymentFailureCount > 0) == true)
                actions.Add("Address payment issues");

            if (SupportInfo?.Any(s => s.OpenTickets > 0) == true)
                actions.Add("Follow up on support tickets");

            if (QuickMetrics.DataCompletenessScore < 50)
                actions.Add("Enrich customer data");

            return actions;
        }
    }

    // Supporting DTOs for enhanced customer detail
    public sealed class CustomerDataSourceDetails
    {
        public List<DataSourceInfo> CrmSources { get; set; } = new();
        public List<DataSourceInfo> PaymentSources { get; set; } = new();
        public List<DataSourceInfo> MarketingSources { get; set; } = new();
        public List<DataSourceInfo> SupportSources { get; set; } = new();
        public List<DataSourceInfo> EngagementSources { get; set; } = new();

        // Summary properties
        public int TotalActiveSources => CrmSources.Count(c => c.IsActive) +
                                        PaymentSources.Count(p => p.IsActive) +
                                        MarketingSources.Count(m => m.IsActive) +
                                        SupportSources.Count(s => s.IsActive) +
                                        EngagementSources.Count(e => e.IsActive);

        public DateTime? LastOverallSync => new[] {
            CrmSources.Where(c => c.IsActive).Max(c => (DateTime?)c.LastSyncedAt),
            PaymentSources.Where(p => p.IsActive).Max(p => (DateTime?)p.LastSyncedAt),
            MarketingSources.Where(m => m.IsActive).Max(m => (DateTime?)m.LastSyncedAt),
            SupportSources.Where(s => s.IsActive).Max(s => (DateTime?)s.LastSyncedAt),
            EngagementSources.Where(e => e.IsActive).Max(e => (DateTime?)e.LastSyncedAt)
        }.Where(d => d.HasValue).DefaultIfEmpty().Max();

        public List<string> UniqueSourceNames => CrmSources.Select(c => c.Source)
            .Union(PaymentSources.Select(p => p.Source))
            .Union(MarketingSources.Select(m => m.Source))
            .Union(SupportSources.Select(s => s.Source))
            .Union(EngagementSources.Select(e => e.Source))
            .Distinct()
            .ToList();
    }

    public sealed class DataSourceInfo
    {
        public string Source { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public bool IsPrimarySource { get; set; }
        public DateTime LastSyncedAt { get; set; }
        public string? ImportBatchId { get; set; }
        public string? ImportedByUserName { get; set; }
        public int SourcePriority { get; set; }
        public bool IsActive { get; set; }
        public string? SyncVersion { get; set; }

        // Calculated properties
        public string SyncStatus => CalculateSyncStatus();
        public int DaysSinceLastSync => (DateTime.UtcNow - LastSyncedAt).Days;
        public string PriorityLevel => SourcePriority switch
        {
            >= 90 => "Critical",
            >= 70 => "High",
            >= 50 => "Medium",
            >= 30 => "Low",
            _ => "Minimal"
        };

        private string CalculateSyncStatus()
        {
            if (!IsActive) return "Inactive";

            var daysSinceSync = (DateTime.UtcNow - LastSyncedAt).TotalDays;
            return daysSinceSync switch
            {
                <= 1 => "Current",
                <= 7 => "Recent",
                <= 30 => "Dated",
                _ => "Stale"
            };
        }
    }

    public sealed class CustomerQuickMetrics
    {
        public int TotalDataSources { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? LastDataSync { get; set; }
        public int TotalActivities { get; set; }
        public int TotalChurnPredictions { get; set; }
        public bool HasRecentActivity { get; set; }
        public bool HasMultipleSources { get; set; }
        public decimal DataCompletenessScore { get; set; }

        // Additional calculated metrics
        public int DaysSinceLastActivity => LastActivityDate.HasValue
            ? (DateTime.UtcNow - LastActivityDate.Value).Days
            : int.MaxValue;

        public int DaysSinceLastSync => LastDataSync.HasValue
            ? (DateTime.UtcNow - LastDataSync.Value).Days
            : int.MaxValue;

        public string ActivityStatus => DaysSinceLastActivity switch
        {
            <= 1 => "Very Active",
            <= 7 => "Active",
            <= 30 => "Moderate",
            <= 90 => "Low",
            _ => "Inactive"
        };

        public string DataFreshnessStatus => DaysSinceLastSync switch
        {
            <= 1 => "Fresh",
            <= 7 => "Current",
            <= 30 => "Acceptable",
            _ => "Stale"
        };

        public string OverallHealthScore => CalculateOverallHealth();

        private string CalculateOverallHealth()
        {
            var score = 0;

            // Data completeness (40% weight)
            if (DataCompletenessScore >= 80) score += 40;
            else if (DataCompletenessScore >= 60) score += 30;
            else if (DataCompletenessScore >= 40) score += 20;
            else score += 10;

            // Recent activity (30% weight)  
            if (HasRecentActivity && DaysSinceLastActivity <= 7) score += 30;
            else if (HasRecentActivity && DaysSinceLastActivity <= 30) score += 20;
            else if (DaysSinceLastActivity <= 90) score += 10;

            // Data freshness (20% weight)
            if (DaysSinceLastSync <= 7) score += 20;
            else if (DaysSinceLastSync <= 30) score += 15;
            else if (DaysSinceLastSync <= 90) score += 10;

            // Multiple sources (10% weight)
            if (HasMultipleSources) score += 10;

            return score switch
            {
                >= 85 => "Excellent",
                >= 70 => "Good",
                >= 50 => "Fair",
                >= 30 => "Poor",
                _ => "Critical"
            };
        }
    }

    public sealed class DataQualityInfo
    {
        public decimal CompletenessScore { get; set; }
        public bool HasMultipleSources { get; set; }
        public DateTime? LastDataSync { get; set; }
        public List<string> MissingCriticalData { get; set; } = new();
        public string DataFreshness { get; set; } = string.Empty;
        public List<string> RecommendedActions { get; set; } = new();

        // Additional quality indicators
        public QualityLevel OverallQuality => CalculateOverallQuality();
        public List<DataQualityIssue> QualityIssues { get; set; } = new();
        public DataQualityTrend QualityTrend { get; set; } = new();

        private QualityLevel CalculateOverallQuality()
        {
            var score = CompletenessScore;

            // Penalize for missing critical data
            score -= MissingCriticalData.Count * 10;

            // Bonus for multiple sources
            if (HasMultipleSources) score += 10;

            // Penalize for stale data
            if (DataFreshness == "Stale") score -= 20;
            else if (DataFreshness == "Fair") score -= 10;

            return score switch
            {
                >= 90 => QualityLevel.Excellent,
                >= 75 => QualityLevel.Good,
                >= 60 => QualityLevel.Fair,
                >= 40 => QualityLevel.Poor,
                _ => QualityLevel.Critical
            };
        }
    }

    public sealed class DataQualityIssue
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QualitySeverity Severity { get; set; }
        public string? SuggestedAction { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    public sealed class DataQualityTrend
    {
        public decimal PreviousScore { get; set; }
        public decimal CurrentScore { get; set; }
        public TrendDirection Direction => CurrentScore > PreviousScore ? TrendDirection.Improving :
                                         CurrentScore < PreviousScore ? TrendDirection.Declining :
                                         TrendDirection.Stable;
        public decimal ChangePercentage => PreviousScore > 0 ?
            Math.Round(((CurrentScore - PreviousScore) / PreviousScore) * 100, 1) : 0;
    }

            // Enums for quality assessment
            public enum QualityLevel
            {
                Critical = 1,
                Poor = 2,
                Fair = 3,
                Good = 4,
                Excellent = 5
            }

            public enum QualitySeverity
            {
                Low = 1,
                Medium = 2,
                High = 3,
                Critical = 4
            }

            public enum TrendDirection
            {
                Declining = -1,
                Stable = 0,
                Improving = 1
            }
}