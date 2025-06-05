using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerCrmInfo
    {
        public string Source { get; set; } = string.Empty;

        public bool IsPrimarySource { get; set; }
        public string? ImportBatchId { get; set; }
        public string? ImportedByUserName { get; set; }
        public string? LeadSource { get; set; }
        public string? LifecycleStage { get; set; }
        public string? SalesOwnerName { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int DealCount { get; set; }
        public decimal TotalDealValue { get; set; }

        public DateTime LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
    }

}
