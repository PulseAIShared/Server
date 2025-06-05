using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Integrations
{
    public class IntegrationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastSyncedAt { get; set; }
        public int SyncedRecordCount { get; set; }
        public DateTime ConfiguredAt { get; set; }
        public string? LastSyncError { get; set; }
        public IntegrationUserResponse? ConfiguredBy { get; set; }
    }

    public class IntegrationUserResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

}
