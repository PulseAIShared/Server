using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Services;
using Domain.Imports;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Commands
{
    internal sealed class CancelImportJobCommandHandler(
         IApplicationDbContext context,
         IUserContext userContext,
         IBackgroundJobClient backgroundJobClient,
         ILogger<CancelImportJobCommandHandler> logger)
         : ICommandHandler<CancelImportJobCommand, bool>
    {
        public async Task<Result<bool>> Handle(CancelImportJobCommand command, CancellationToken cancellationToken)
        {
            var importJob = await context.ImportJobs
                .FirstOrDefaultAsync(ij => ij.Id == command.ImportJobId, cancellationToken);

            if (importJob == null)
            {
                return Result.Failure<bool>(ImportJobErrors.NotFound(command.ImportJobId));
            }

            if (importJob.UserId != userContext.UserId)
            {
                return Result.Failure<bool>(ImportJobErrors.Unauthorized());
            }

            if (!importJob.CanBeCancelled)
            {
                return Result.Failure<bool>(ImportJobErrors.CannotCancel(command.ImportJobId));
            }

            try
            {
                // Queue cancellation job with Hangfire
                var jobId = backgroundJobClient.Enqueue<IImportBackgroundService>(
                    "imports", // Use specific queue for imports
                    service => service.CancelImportAsync(importJob.Id));

                logger.LogInformation("Queued cancellation for import job {ImportJobId} for user {UserId}, Hangfire job {HangfireJobId}",
                    command.ImportJobId, userContext.UserId, jobId);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue cancellation for import job {ImportJobId}", command.ImportJobId);
                return Result.Failure<bool>(Error.Problem(
                    "ImportJob.CancelFailed",
                    "Failed to cancel import job. Please try again."));
            }
        }
    }
}
