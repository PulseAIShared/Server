using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Services;
using Domain.Imports;
using Domain.Users;
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

    internal sealed class CreateImportJobCommandHandler(
           IApplicationDbContext context,
           IUserContext userContext,
           IFileStorageService fileStorageService,
           IBackgroundJobClient backgroundJobClient,
           ILogger<CreateImportJobCommandHandler> logger)
           : ICommandHandler<CreateImportJobCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateImportJobCommand command, CancellationToken cancellationToken)
        {
            try
            {
                // Verify user exists and get company
                var user = await context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

                if (user == null)
                {
                    return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
                }

                if (user.CompanyId == null)
                {
                    return Result.Failure<Guid>(Error.Problem(
                        "User.NoCompany",
                        "User must be associated with a company to import customers"));
                }

                // Generate unique file path
                var fileExtension = Path.GetExtension(command.File.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = await fileStorageService.SaveFileAsync(
                    command.File,
                    "imports",
                    fileName);

                // Create import job with skipDuplicates metadata
                var importJob = new ImportJob
                {
                    UserId = command.UserId,
                    CompanyId = user.CompanyId,
                    FileName = command.File.FileName,
                    FilePath = filePath,
                    Type = command.Type,
                    ImportSource = command.ImportSource,
                    Status = ImportJobStatus.Pending
                };

                // Store the skipDuplicates setting in ImportSource for now
                // In a real implementation, you might add a separate property to ImportJob
                if (command.SkipDuplicates)
                {
                    importJob.ImportSource = $"{command.ImportSource}|skipDuplicates=true";
                }

                context.ImportJobs.Add(importJob);
                await context.SaveChangesAsync(cancellationToken);

                // Queue validation job with Hangfire
                var jobId = backgroundJobClient.Enqueue<IImportBackgroundService>(
                    "imports",
                    service => service.ValidateImportAsync(importJob.Id, command.SkipDuplicates));

                logger.LogInformation("Created import job {ImportJobId} for user {UserId} (skipDuplicates: {SkipDuplicates}), queued validation job {HangfireJobId}",
                    importJob.Id, command.UserId, command.SkipDuplicates, jobId);

                return importJob.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create import job for user {UserId}", command.UserId);
                return Result.Failure<Guid>(Error.Problem(
                    "ImportJob.CreateFailed",
                    "Failed to create import job. Please try again."));
            }
        }
    }

}