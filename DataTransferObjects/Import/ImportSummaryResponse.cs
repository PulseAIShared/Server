using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Import
{
    public sealed class ImportSummaryResponse
    {
        public decimal AverageRevenue { get; set; }
        public double AverageTenureMonths { get; set; }
        public int NewCustomers { get; set; }
        public int HighRiskCustomers { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }
}
