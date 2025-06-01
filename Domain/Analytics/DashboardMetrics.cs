using Domain.Users;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Analytics
{
    public class DashboardMetrics : Entity
    {
        [Required]
        public Guid CompanyId { get; set; } = Guid.Empty;

        public DateTime MetricDate { get; set; }

        public int TotalCustomers { get; set; }

        public decimal ChurnRate { get; set; }

        public decimal RevenueRecovered { get; set; }

        public decimal AverageLifetimeValue { get; set; }

        public int HighRiskCustomers { get; set; }

        public int ActiveCampaigns { get; set; }

        public decimal CampaignSuccessRate { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
    }
}
