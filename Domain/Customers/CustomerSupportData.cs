using SharedKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Customers
{
    public sealed class CustomerSupportData : Entity
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public string Source { get; set; } = string.Empty; // zendesk, intercom, freshdesk, manual_import, etc.

        [Required]
        public string ExternalId { get; set; } = string.Empty;

        public bool IsPrimarySource { get; set; } = false;
        public int SourcePriority { get; set; } = 0;

        // Support metrics
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
        public DateTime? FirstTicketDate { get; set; }
        public DateTime? LastTicketDate { get; set; }
        public double AverageResolutionTime { get; set; } // in hours
        public double CustomerSatisfactionScore { get; set; }

        // Ticket priority breakdown
        public int LowPriorityTickets { get; set; }
        public int MediumPriorityTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int UrgentTickets { get; set; }

        // Support categories
        public Dictionary<string, int>? TicketsByCategory { get; set; }

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
