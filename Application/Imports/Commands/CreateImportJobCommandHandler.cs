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

namespace Application.Imports.Commands
{
    internal sealed class CreateImportJobCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IFileStorageService fileStorageService,
        ICustomerImportService customerImportService,
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

                // Create import job
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

                context.ImportJobs.Add(importJob);
                await context.SaveChangesAsync(cancellationToken);

                // Start validation in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await customerImportService.ValidateImportFileAsync(importJob.Id, command.SkipDuplicates);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to validate import file for job {ImportJobId}", importJob.Id);
                    }
                }, cancellationToken);

                logger.LogInformation("Created import job {ImportJobId} for user {UserId}", importJob.Id, command.UserId);

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
