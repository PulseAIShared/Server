using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Import
{
    public sealed class ImportJobResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public ImportJobStatus Status { get; set; }
        public ImportJobType Type { get; set; }
        public string? ImportSource { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public int SkippedRecords { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double ProgressPercentage { get; set; }
        public List<ImportErrorResponse> ValidationErrors { get; set; } = new();
        public ImportSummaryResponse? Summary { get; set; }
    }
}
