using SharedKernel.Enums;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public class ImportJob : Entity
    {
        public Guid UserId { get; set; }
        public Guid CompanyId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;
        public ImportJobType Type { get; set; }
        public string? ImportSource { get; set; } // hubspot, salesforce, etc.

        // Progress tracking
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public int SkippedRecords { get; set; }

        // Error tracking
        public string? ErrorMessage { get; set; }
        public string? ValidationErrors { get; set; } // JSON array of validation errors

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Results
        public string? ImportSummary { get; set; } // JSON summary of insights

        // Navigation properties
        public Domain.Users.User User { get; set; } = null!;
        public Domain.Users.Company Company { get; set; } = null!;

        // Business methods
        public void Start()
        {
            Status = ImportJobStatus.Processing;
            StartedAt = DateTime.UtcNow;
        }

        public void Complete(ImportJobResult result)
        {
            Status = ImportJobStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            TotalRecords = result.TotalRecords;
            ProcessedRecords = result.ProcessedRecords;
            SuccessfulRecords = result.SuccessfulRecords;
            FailedRecords = result.FailedRecords;
            SkippedRecords = result.SkippedRecords;
            ImportSummary = System.Text.Json.JsonSerializer.Serialize(result.Summary);
        }

        public void Fail(string errorMessage)
        {
            Status = ImportJobStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
        }

        public void Cancel()
        {
            Status = ImportJobStatus.Cancelled;
            CompletedAt = DateTime.UtcNow;
        }

        public void UpdateProgress(int processed, int successful, int failed, int skipped)
        {
            ProcessedRecords = processed;
            SuccessfulRecords = successful;
            FailedRecords = failed;
            SkippedRecords = skipped;
        }

        public void SetValidationErrors(List<ImportError> errors)
        {
            ValidationErrors = System.Text.Json.JsonSerializer.Serialize(errors);
        }

        public List<ImportError> GetValidationErrors()
        {
            if (string.IsNullOrEmpty(ValidationErrors))
                return new List<ImportError>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<ImportError>>(ValidationErrors) ?? new List<ImportError>();
            }
            catch
            {
                return new List<ImportError>();
            }
        }

        public ImportSummary? GetImportSummary()
        {
            if (string.IsNullOrEmpty(ImportSummary))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ImportSummary>(ImportSummary);
            }
            catch
            {
                return null;
            }
        }

        public double GetProgressPercentage()
        {
            if (TotalRecords == 0) return 0;
            return (double)ProcessedRecords / TotalRecords * 100;
        }

        public bool IsCompleted => Status == ImportJobStatus.Completed || Status == ImportJobStatus.Failed || Status == ImportJobStatus.Cancelled;

        public bool CanBeCancelled => Status == ImportJobStatus.Pending || Status == ImportJobStatus.Validating;

        public bool HasErrors => FailedRecords > 0 || !string.IsNullOrEmpty(ErrorMessage);
    }
}
