using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Services;
using Domain.Imports;
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
       ICustomerImportService customerImportService,
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
                await customerImportService.CancelImportJobAsync(command.ImportJobId);
                logger.LogInformation("Cancelled import job {ImportJobId} for user {UserId}", command.ImportJobId, userContext.UserId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cancel import job {ImportJobId}", command.ImportJobId);
                return Result.Failure<bool>(Error.Problem(
                    "ImportJob.CancelFailed",
                    "Failed to cancel import job. Please try again."));
            }
        }
    }
}
