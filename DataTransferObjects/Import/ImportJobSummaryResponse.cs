using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Import
{
    public sealed class ImportJobSummaryResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public ImportJobStatus Status { get; set; }
        public ImportJobType Type { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
