using Infrastructure.Integrations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Models
{
    public class SyncOptions
    {
        public bool IncrementalSync { get; set; } = true;
        public DateTime? SyncFromDate { get; set; }
        public int BatchSize { get; set; } = 100;
    }

    public class SyncResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int NewRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public int ErrorRecords { get; set; }
        public List<SyncError> Errors { get; set; } = new();
    }

    public class SyncError
    {
        public string? RecordId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }

    public class HubSpotContactsResponse
    {
        public List<HubSpotContact> Results { get; set; } = new();
        public HubSpotPaging? Paging { get; set; }
    }

    public class HubSpotPaging
    {
        public HubSpotNext? Next { get; set; }
    }

    public class HubSpotNext
    {
        public string After { get; set; } = string.Empty;
    }
}
