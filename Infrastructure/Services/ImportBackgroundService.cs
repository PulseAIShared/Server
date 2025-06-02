using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ImportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ImportBackgroundService> logger) : IImportBackgroundService
    {
        public async Task ValidateImportAsync(Guid jobId, bool skipDuplicates)
        {
            using var scope = serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<ICustomerImportService>();

            try
            {
                logger.LogInformation("Starting background validation for import job {JobId}", jobId);
                await importService.ValidateImportFileAsync(jobId, skipDuplicates);
                logger.LogInformation("Completed background validation for import job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate import job {JobId} in background", jobId);
                throw; // Re-throw so Hangfire can handle retries
            }
        }

        public async Task ProcessImportAsync(Guid jobId)
        {
            using var scope = serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<ICustomerImportService>();

            try
            {
                logger.LogInformation("Starting background processing for import job {JobId}", jobId);
                await importService.ProcessImportFileAsync(jobId);
                logger.LogInformation("Completed background processing for import job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process import job {JobId} in background", jobId);
                throw; // Re-throw so Hangfire can handle retries
            }
        }

        public async Task CancelImportAsync(Guid jobId)
        {
            using var scope = serviceProvider.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<ICustomerImportService>();

            try
            {
                logger.LogInformation("Starting background cancellation for import job {JobId}", jobId);
                await importService.CancelImportJobAsync(jobId);
                logger.LogInformation("Completed background cancellation for import job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cancel import job {JobId} in background", jobId);
                throw; // Re-throw so Hangfire can handle retries
            }
        }
    }
}
