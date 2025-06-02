using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Services;
using Domain.Imports;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

namespace Application.Imports.Commands
{
    internal sealed class ConfirmImportJobCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IBackgroundJobClient backgroundJobClient,
        ILogger<ConfirmImportJobCommandHandler> logger)
        : ICommandHandler<ConfirmImportJobCommand, bool>
    {
        public async Task<Result<bool>> Handle(ConfirmImportJobCommand command, CancellationToken cancellationToken)
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

            if (importJob.Status != ImportJobStatus.Pending)
            {
                return Result.Failure<bool>(Error.Problem(
                    "ImportJob.InvalidStatus",
                    $"Cannot confirm import job in {importJob.Status} status"));
            }

            // Queue processing job with Hangfire
            var jobId = backgroundJobClient.Enqueue<IImportBackgroundService>(
                "imports", // Use specific queue for imports
                service => service.ProcessImportAsync(importJob.Id));

            logger.LogInformation("Confirmed import job {ImportJobId} for user {UserId}, queued processing job {HangfireJobId}",
                command.ImportJobId, userContext.UserId, jobId);

            return true;
        }
    }
}