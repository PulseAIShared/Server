using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{

    public sealed class CustomerSupportInfo
    {
        public string Source { get; set; } = string.Empty;
        public bool IsPrimarySource { get; set; }
        public string? ImportBatchId { get; set; }
        public string? ImportedByUserName { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
        public DateTime? FirstTicketDate { get; set; }
        public DateTime? LastTicketDate { get; set; }
        public double AverageResolutionTime { get; set; }
        public double CustomerSatisfactionScore { get; set; }
        public int LowPriorityTickets { get; set; }
        public int MediumPriorityTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int UrgentTickets { get; set; }
        public Dictionary<string, int>? TicketsByCategory { get; set; }
        public DateTime LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
    }

}
