using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ICustomerImportService
    {
        /// <summary>
        /// Validates the uploaded CSV file and checks for data issues
        /// </summary>
        /// <param name="jobId">The import job ID</param>
        /// <param name="skipDuplicates">Whether to skip duplicate email addresses</param>
        Task ValidateImportFileAsync(Guid jobId, bool skipDuplicates);

        /// <summary>
        /// Processes the validated CSV file and imports customers
        /// </summary>
        /// <param name="jobId">The import job ID</param>
        Task ProcessImportFileAsync(Guid jobId);

        /// <summary>
        /// Cancels an in-progress import job
        /// </summary>
        /// <param name="jobId">The import job ID</param>
        Task CancelImportJobAsync(Guid jobId);

        /// <summary>
        /// Retries a failed import job
        /// </summary>
        /// <param name="jobId">The import job ID</param>
        Task RetryImportJobAsync(Guid jobId);

        /// <summary>
        /// Cleans up old import jobs and their associated files
        /// </summary>
        /// <param name="olderThanDays">Delete jobs older than this many days</param>
        Task CleanupOldImportJobsAsync(int olderThanDays = 30);
    }
}
