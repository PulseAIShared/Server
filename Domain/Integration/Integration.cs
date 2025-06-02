using Domain.Users;
using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Integration
{
    public class Integration : Entity
    {
        [Required]
        public Guid CompanyId { get; set; } = Guid.Empty;  // Changed from UserId to CompanyId

        [Required]
        public IntegrationType Type { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public IntegrationStatus Status { get; set; } = IntegrationStatus.Disconnected;

        public Dictionary<string, string>? Configuration { get; set; }

        public Dictionary<string, string>? Credentials { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        public int SyncedRecordCount { get; set; }

        public string? LastSyncError { get; set; }

        public Guid ConfiguredByUserId { get; set; }
        public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;

        public Company Company { get; set; } = null!;
        public User ConfiguredBy { get; set; } = null!;
    }
}
