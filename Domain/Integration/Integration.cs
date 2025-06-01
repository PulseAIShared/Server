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
        public Guid UserId { get; set; } = Guid.Empty;

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

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
