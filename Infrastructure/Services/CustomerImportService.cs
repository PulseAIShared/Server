using Application.Abstractions.Data;
using Application.Services;
using Domain.Customers;
using Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using System.Linq;
using System.Text.RegularExpressions;
using Infrastructure.Integrations.Models;

namespace Infrastructure.Services;

public class CustomerImportService : ICustomerImportService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileStorageService _fileStorageService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CustomerImportService> _logger;

    private const int BATCH_SIZE = 100;
    private const int PROGRESS_UPDATE_INTERVAL = 50;

    public CustomerImportService(
        IServiceProvider serviceProvider,
        IFileStorageService fileStorageService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CustomerImportService> logger)
    {
        _serviceProvider = serviceProvider;
        _fileStorageService = fileStorageService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    // Fixed ProcessImportFileAsync method with proper error handling using fresh context
    public async Task ProcessImportFileAsync(Guid jobId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var customerAggregationService = scope.ServiceProvider.GetRequiredService<ICustomerAggregationService>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Import job {JobId} not found for processing", jobId);
            return;
        }

        try
        {
            _logger.LogInformation("Starting processing for import job {JobId}", jobId);

            job.Start();
            job.Raise(new ImportJobStartedDomainEvent(
                job.Id, job.UserId, job.FileName, job.Type, job.ImportSource));

            await context.SaveChangesAsync();

            // Determine skipDuplicates from the job (stored during creation)
            bool skipDuplicates = job.ShouldSkipDuplicates();

            // Read and parse CSV file again
            var fileContent = await _fileStorageService.ReadFileAsync(job.FilePath);
            var (customers, totalRecords, _) = ParseCsvFile(fileContent, job.ImportSource!);

            var result = new ImportJobResult
            {
                TotalRecords = totalRecords,
                ProcessedRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0,
                SkippedRecords = 0,
                UpdatedRecords = 0,
                NewRecords = 0,
                Errors = new List<ImportError>(),
                Updates = new List<ImportUpdate>()
            };

            // Get existing customers for duplicate checking
            var emails = customers.Select(c => c.Email.ToLower()).ToList();
            var existingCustomers = await context.Customers
                .Where(c => c.CompanyId == job.CompanyId && emails.Contains(c.Email.ToLower()))
                .ToDictionaryAsync(c => c.Email.ToLower(), c => c);

            _logger.LogInformation("Processing {TotalCustomers} customers, {ExistingCount} already exist (skipDuplicates: {SkipDuplicates})",
                customers.Count, existingCustomers.Count, skipDuplicates);

            // Process in batches
            for (int i = 0; i < customers.Count; i += BATCH_SIZE)
            {
                var batch = customers.Skip(i).Take(BATCH_SIZE).ToList();
                await ProcessBatchWithDuplicateHandling(batch, job, result, existingCustomers, i, context, skipDuplicates, customerAggregationService);

                // Update progress periodically
                if (i % PROGRESS_UPDATE_INTERVAL == 0 || i + BATCH_SIZE >= customers.Count)
                {
                    job.UpdateProgressDetailed(
                        Math.Min(i + BATCH_SIZE, customers.Count),
                        result.SuccessfulRecords,
                        result.FailedRecords,
                        result.SkippedRecords,
                        result.UpdatedRecords,
                        result.NewRecords
                    );

                    job.Raise(new ImportJobProgressDomainEvent(
                        job.Id, job.UserId, job.ProcessedRecords, job.TotalRecords, job.GetProgressPercentage()));

                    await context.SaveChangesAsync();

                    _logger.LogDebug("Import job {JobId} progress: {ProcessedRecords}/{TotalRecords} ({ProgressPercentage:F1}%) - New: {NewRecords}, Updated: {UpdatedRecords}, Skipped: {SkippedRecords}",
                        jobId, job.ProcessedRecords, job.TotalRecords, job.GetProgressPercentage(), result.NewRecords, result.UpdatedRecords, result.SkippedRecords);
                }
            }

            // Calculate final insights
            var importedCustomers = await context.Customers
                .Where(c => c.CompanyId == job.CompanyId && c.LastSyncedAt >= job.StartedAt)
                .ToListAsync();

            result.Summary = CalculateInsights(importedCustomers);

            job.Complete(result);

            // Raise domain event for completed import
            job.Raise(new ImportJobCompletedDomainEvent(
                job.Id, job.UserId, ImportJobStatus.Completed,
                result.TotalRecords, result.SuccessfulRecords, result.FailedRecords, result.SkippedRecords,
                null, new ImportSummary(result.Summary.AverageRevenue, result.Summary.AverageTenureMonths,
                    result.Summary.NewCustomers, result.Summary.HighRiskCustomers)));

            await context.SaveChangesAsync();

            // Cleanup
            try
            {
                await _fileStorageService.DeleteFileAsync(job.FilePath);
                _logger.LogInformation("Cleaned up import file {FilePath} for job {JobId}", job.FilePath, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete import file {FilePath} for job {JobId}", job.FilePath, jobId);
            }

            _logger.LogInformation("Import job {JobId} completed successfully. " +
                "{NewRecords} new records, {UpdatedRecords} updated, {FailedRecords} failed, {SkippedRecords} skipped",
                jobId, result.NewRecords, result.UpdatedRecords, result.FailedRecords, result.SkippedRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process import file for job {JobId}. Error: {Error}", jobId, ex.Message);

            // Log inner exceptions
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                _logger.LogError("Inner exception: {InnerError}", innerEx.Message);
                innerEx = innerEx.InnerException;
            }

            // Use a FRESH context to update the job status to avoid the same error
            try
            {
                using var freshScope = _serviceProvider.CreateScope();
                var freshContext = freshScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                // Get the job with the fresh context
                var freshJob = await freshContext.ImportJobs.FindAsync(jobId);
                if (freshJob != null)
                {
                    // Update the job status to failed
                    freshJob.Fail($"Import failed: {ex.Message}");

                    // Raise failure event
                    freshJob.Raise(new ImportJobCompletedDomainEvent(
                        freshJob.Id, freshJob.UserId, ImportJobStatus.Failed,
                        freshJob.TotalRecords, freshJob.SuccessfulRecords, freshJob.FailedRecords, freshJob.SkippedRecords,
                        ex.Message, null));

                    await freshContext.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated import job {JobId} status to Failed using fresh context", jobId);
                }
                else
                {
                    _logger.LogError("Could not find import job {JobId} in fresh context to update status", jobId);
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to update job status to failed for job {JobId} even with fresh context. Error: {SaveError}",
                    jobId, saveEx.Message);

                // Try direct SQL update as last resort
                try
                {
                    using var directScope = _serviceProvider.CreateScope();
                    var directContext = directScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    await directContext.Database.ExecuteSqlRawAsync(
                        "UPDATE import_jobs SET status = {0}, error_message = {1}, completed_at = {2} WHERE id = {3}",
                        (int)ImportJobStatus.Failed,
                        $"Import failed: {ex.Message}",
                        DateTime.UtcNow,
                        jobId);

                    _logger.LogInformation("Successfully updated import job {JobId} status to Failed using direct SQL", jobId);
                }
                catch (Exception sqlEx)
                {
                    _logger.LogError(sqlEx, "Failed to update job status even with direct SQL for job {JobId}", jobId);
                }
            }

            throw;
        }
    }

    // Also fix ValidateImportFileAsync with the same pattern
    public async Task ValidateImportFileAsync(Guid jobId, bool skipDuplicates)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Import job {JobId} not found for validation", jobId);
            return;
        }

        try
        {
            _logger.LogInformation("Starting validation for import job {JobId} (skipDuplicates: {SkipDuplicates})",
                jobId, skipDuplicates);

            job.Status = ImportJobStatus.Validating;
            await context.SaveChangesAsync();

            // Read and parse CSV file
            var fileContent = await _fileStorageService.ReadFileAsync(job.FilePath);
            _logger.LogInformation("Read file content, length: {Length} characters", fileContent.Length);

            var (customers, totalRecords, parseErrors) = ParseCsvFile(fileContent, job.ImportSource!);
            _logger.LogInformation("Parsed {CustomerCount} customers from {TotalRecords} records", customers.Count, totalRecords);

            job.TotalRecords = totalRecords;
            var allErrors = new List<ImportError>(parseErrors);

            // Check for duplicates based on skipDuplicates setting
            var duplicateInfo = await AnalyzeDuplicatesAsync(customers, job.CompanyId, context, skipDuplicates);

            if (skipDuplicates)
            {
                _logger.LogInformation("Skip duplicates enabled: {SkippedCount} existing customers will be skipped",
                    duplicateInfo.SkippedEmails.Count);
            }
            else
            {
                _logger.LogInformation("Skip duplicates disabled: {UpdateCount} existing customers will be updated",
                    duplicateInfo.ExistingEmails.Count);
            }

            // Store validation errors (only actual parsing/validation errors, not duplicates)
            if (allErrors.Count > 0)
            {
                job.SetValidationErrors(allErrors);
                _logger.LogWarning("Import job {JobId} has {ErrorCount} validation errors", jobId, allErrors.Count);
            }

            // Raise validation completed event
            job.Raise(new ImportJobValidationCompletedDomainEvent(
                job.Id,
                job.UserId,
                totalRecords,
                allErrors.Count,
                allErrors.Count > 0
            ));

            job.Status = ImportJobStatus.Pending;
            await context.SaveChangesAsync();

            // AUTO-PROCESS: Queue processing job immediately after successful validation
            if (allErrors.Count == 0 || !allErrors.Any(e => IsCriticalError(e)))
            {
                _logger.LogInformation("Validation successful for job {JobId}, auto-queuing processing", jobId);

                var processingJobId = _backgroundJobClient.Enqueue<IImportBackgroundService>(
                    "imports",
                    service => service.ProcessImportAsync(jobId));

                _logger.LogInformation("Auto-queued processing job {HangfireJobId} for import {ImportJobId}",
                    processingJobId, jobId);
            }
            else
            {
                _logger.LogWarning("Import job {JobId} has critical errors, skipping auto-processing", jobId);
            }

            _logger.LogInformation("Validation completed for import job {JobId}. Total: {TotalRecords}, Errors: {ErrorCount}",
                jobId, totalRecords, allErrors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate import file for job {JobId}. Error: {Error}", jobId, ex.Message);

            // Use a FRESH context to update the job status
            try
            {
                using var freshScope = _serviceProvider.CreateScope();
                var freshContext = freshScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var freshJob = await freshContext.ImportJobs.FindAsync(jobId);
                if (freshJob != null)
                {
                    freshJob.Fail($"Validation failed: {ex.Message}");
                    await freshContext.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated import job {JobId} status to Failed during validation using fresh context", jobId);
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to update job status to failed for job {JobId} even with fresh context during validation", jobId);
            }

            throw;
        }
    }

    public async Task CancelImportJobAsync(Guid jobId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Import job {JobId} not found for cancellation", jobId);
            return;
        }

        if (!job.CanBeCancelled)
        {
            _logger.LogWarning("Import job {JobId} cannot be cancelled in status {Status}", jobId, job.Status);
            return;
        }

        try
        {
            job.Cancel();
            await context.SaveChangesAsync();

            try
            {
                await _fileStorageService.DeleteFileAsync(job.FilePath);
                _logger.LogInformation("Cleaned up file {FilePath} for cancelled job {JobId}", job.FilePath, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {FilePath} for cancelled job {JobId}", job.FilePath, jobId);
            }

            _logger.LogInformation("Cancelled import job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel import job {JobId}", jobId);
            throw;
        }
    }

    public async Task RetryImportJobAsync(Guid jobId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Import job {JobId} not found for retry", jobId);
            return;
        }

        if (job.Status != ImportJobStatus.Failed)
        {
            _logger.LogWarning("Cannot retry import job {JobId} in status {Status}", jobId, job.Status);
            return;
        }

        try
        {
            // Reset job status and clear error data
            job.Status = ImportJobStatus.Pending;
            job.ErrorMessage = null;
            job.ValidationErrors = null;
            job.ImportUpdates = null;
            job.ProcessedRecords = 0;
            job.SuccessfulRecords = 0;
            job.FailedRecords = 0;
            job.SkippedRecords = 0;
            job.UpdatedRecords = 0;
            job.NewRecords = 0;
            job.StartedAt = null;
            job.CompletedAt = null;

            await context.SaveChangesAsync();

            _logger.LogInformation("Reset import job {JobId} for retry", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry import job {JobId}", jobId);
            throw;
        }
    }

    public async Task CleanupOldImportJobsAsync(int olderThanDays = 30)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            var oldJobs = await context.ImportJobs
                .Where(ij => ij.CreatedAt < cutoffDate &&
                            (ij.Status == ImportJobStatus.Completed ||
                             ij.Status == ImportJobStatus.Failed ||
                             ij.Status == ImportJobStatus.Cancelled))
                .ToListAsync();

            foreach (var job in oldJobs)
            {
                try
                {
                    if (await _fileStorageService.FileExistsAsync(job.FilePath))
                    {
                        await _fileStorageService.DeleteFileAsync(job.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FilePath} for job {JobId}", job.FilePath, job.Id);
                }
            }

            context.ImportJobs.RemoveRange(oldJobs);
            await context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old import jobs older than {Days} days", oldJobs.Count, olderThanDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old import jobs");
            throw;
        }
    }

    // Helper methods
    private static bool IsCriticalError(ImportError error)
    {
        var criticalMessages = new[]
        {
            "Email is required",
            "Invalid email format",
            "Either first name or last name is required"
        };

        return criticalMessages.Any(msg => error.ErrorMessage.Contains(msg, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<DuplicateAnalysisResult> AnalyzeDuplicatesAsync(
        List<CustomerImportData> customers,
        Guid companyId,
        IApplicationDbContext context,
        bool skipDuplicates)
    {
        var emails = customers.Select(c => c.Email.ToLower()).ToList();
        var existingEmails = await context.Customers
            .Where(c => c.CompanyId == companyId && emails.Contains(c.Email.ToLower()))
            .Select(c => c.Email.ToLower())
            .ToHashSetAsync();

        return new DuplicateAnalysisResult
        {
            ExistingEmails = existingEmails,
            SkippedEmails = skipDuplicates ? existingEmails : new HashSet<string>()
        };
    }

    private async Task ProcessBatchWithDuplicateHandling(
    List<CustomerImportData> batch,
    ImportJob job,
    ImportJobResult result,
    Dictionary<string, Customer> existingCustomers,
    int batchStartIndex,
    IApplicationDbContext context,
    bool skipDuplicates,
    ICustomerAggregationService customerAggregationService)
    {
        foreach (var (importData, index) in batch.Select((c, i) => (c, i)))
        {
            var emailKey = importData.Email.ToLower();
            var rowNumber = batchStartIndex + index + 2; // +2 for header and 1-based indexing

            try
            {
                if (existingCustomers.TryGetValue(emailKey, out var existingCustomer))
                {
                    if (skipDuplicates)
                    {
                        // Skip this customer entirely
                        result.SkippedRecords++;
                        _logger.LogDebug("Skipped existing customer {Email} at row {RowNumber} in job {JobId}",
                            importData.Email, rowNumber, job.Id);
                    }
                    else
                    {
                        // Update existing customer using aggregation service
                        var sourceData = ConvertImportDataToSourceData(importData);

                        try
                        {
                            _logger.LogDebug("Updating existing customer {Email} with aggregation service", importData.Email);

                            await customerAggregationService.AddOrUpdateCustomerDataAsync(
                                existingCustomer.Id,
                                sourceData,
                                job.ImportSource ?? "manual_import",
                                job.Id.ToString(),
                                job.UserId
                            );

                            result.UpdatedRecords++;
                            _logger.LogDebug("Successfully updated existing customer {Email} using aggregation service in job {JobId}",
                                importData.Email, job.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update customer {Email} via aggregation service. Error: {Error}",
                                importData.Email, ex.Message);

                            // Log the inner exception details
                            var innerEx = ex.InnerException;
                            while (innerEx != null)
                            {
                                _logger.LogError("Inner exception: {InnerError}", innerEx.Message);
                                innerEx = innerEx.InnerException;
                            }

                            result.FailedRecords++;
                            result.Errors.Add(new ImportError
                            {
                                RowNumber = rowNumber,
                                Email = importData.Email,
                                ErrorMessage = $"Update failed: {ex.Message}",
                                FieldName = "AggregationService",
                                ErrorTime = DateTime.UtcNow
                            });
                            continue;
                        }
                    }

                    result.SuccessfulRecords++;
                }
                else
                {
                    try
                    {
                        _logger.LogDebug("Creating new customer {Email}", importData.Email);

                        // Create new customer
                        var newCustomer = CreateCustomerFromImportData(importData, job);

                        context.Customers.Add(newCustomer);

                        _logger.LogDebug("Saving new customer {Email} to get ID", importData.Email);
                        await context.SaveChangesAsync(); // Save to get the ID

                        _logger.LogDebug("New customer {Email} saved with ID {CustomerId}", importData.Email, newCustomer.Id);

                        // Now add the import data using aggregation service
                        var sourceData = ConvertImportDataToSourceData(importData);

                        try
                        {
                            _logger.LogDebug("Adding aggregated data for new customer {Email}", importData.Email);

                            await customerAggregationService.AddOrUpdateCustomerDataAsync(
                                newCustomer.Id,
                                sourceData,
                                job.ImportSource ?? "manual_import",
                                job.Id.ToString(),
                                job.UserId
                            );

                            result.NewRecords++;
                            result.SuccessfulRecords++;
                            existingCustomers[emailKey] = newCustomer; // Add to local cache

                            _logger.LogDebug("Successfully added new customer {Email} with aggregated data at row {RowNumber} in job {JobId}",
                                importData.Email, rowNumber, job.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to add aggregated data for new customer {Email}. Error: {Error}",
                                importData.Email, ex.Message);

                            // Log the inner exception details
                            var innerEx = ex.InnerException;
                            while (innerEx != null)
                            {
                                _logger.LogError("Inner exception: {InnerError}", innerEx.Message);
                                innerEx = innerEx.InnerException;
                            }

                            // Customer was created but aggregated data failed - still count as success
                            result.NewRecords++;
                            result.SuccessfulRecords++;
                            existingCustomers[emailKey] = newCustomer; // Add to local cache anyway
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create new customer {Email}. Error: {Error}",
                            importData.Email, ex.Message);

                        // Log the inner exception details
                        var innerEx = ex.InnerException;
                        while (innerEx != null)
                        {
                            _logger.LogError("Inner exception: {InnerError}", innerEx.Message);
                            innerEx = innerEx.InnerException;
                        }

                        result.FailedRecords++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            Email = importData.Email,
                            ErrorMessage = $"Customer creation failed: {ex.Message}",
                            FieldName = "CustomerCreation",
                            ErrorTime = DateTime.UtcNow
                        });
                        continue;
                    }
                }

                result.ProcessedRecords++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing customer {Email} at row {RowNumber}. Error: {Error}",
                    importData.Email, rowNumber, ex.Message);

                // Log the inner exception details
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    _logger.LogError("Inner exception: {InnerError}", innerEx.Message);
                    innerEx = innerEx.InnerException;
                }

                result.FailedRecords++;
                var error = new ImportError
                {
                    RowNumber = rowNumber,
                    Email = importData.Email,
                    ErrorMessage = ex.Message,
                    FieldName = "General",
                    RawData = $"FirstName: {importData.FirstName}, LastName: {importData.LastName}, Email: {importData.Email}",
                    ErrorTime = DateTime.UtcNow
                };

                result.Errors.Add(error);
            }
        }

        try
        {
            _logger.LogDebug("Saving batch changes for {BatchSize} customers", batch.Count);
            await context.SaveChangesAsync();
            _logger.LogDebug("Successfully saved batch changes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch changes. Error: {Error}", ex.Message);

            // Log the inner exception details
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                _logger.LogError("Inner exception: {InnerError}", innerEx.Message);
                innerEx = innerEx.InnerException;
            }

            throw; // Re-throw to be handled by the calling method
        }
    }


    private Customer CreateCustomerFromImportData(CustomerImportData importData, ImportJob job)
    {
        var customer = new Customer
        {
            CompanyId = job.CompanyId,
            Email = importData.Email,
            FirstName = importData.FirstName ?? "",
            LastName = importData.LastName ?? "",
            Phone = importData.Phone,
            CompanyName = importData.CompanyName,
            JobTitle = importData.JobTitle,
            Location = importData.Location,
            Country = importData.Country,
            TimeZone = importData.TimeZone,
            Age = importData.Age,
            Gender = importData.Gender,
            LastSyncedAt = DateTime.UtcNow,
            ChurnRiskLevel = ChurnRiskLevel.Low,
            ChurnRiskScore = 0
        };

        // Set defaults for required fields if not provided
        if (string.IsNullOrEmpty(customer.FirstName) && string.IsNullOrEmpty(customer.LastName))
        {
            customer.FirstName = "Unknown";
            customer.LastName = "Customer";
        }

        return customer;
    }

 private Dictionary<string, object> ConvertImportDataToSourceData(CustomerImportData importData)
{
    var sourceData = new Dictionary<string, object>();

    // Basic info
    if (!string.IsNullOrEmpty(importData.Email))
        sourceData["email"] = importData.Email;
    if (!string.IsNullOrEmpty(importData.FirstName))
        sourceData["first_name"] = importData.FirstName;
    if (!string.IsNullOrEmpty(importData.LastName))
        sourceData["last_name"] = importData.LastName;
    if (!string.IsNullOrEmpty(importData.Phone))
        sourceData["phone"] = importData.Phone;
    if (!string.IsNullOrEmpty(importData.CompanyName))
        sourceData["company_name"] = importData.CompanyName;
    if (!string.IsNullOrEmpty(importData.JobTitle))
        sourceData["job_title"] = importData.JobTitle;

    // Payment data
    if (importData.SubscriptionStatus.HasValue)
        sourceData["subscription_status"] = importData.SubscriptionStatus.Value.ToString();
    if (importData.Plan.HasValue)
        sourceData["plan"] = importData.Plan.Value.ToString();
    if (importData.MonthlyRecurringRevenue.HasValue)
        sourceData["mrr"] = importData.MonthlyRecurringRevenue.Value;
    if (importData.LifetimeValue.HasValue)
        sourceData["lifetime_value"] = importData.LifetimeValue.Value;
    if (importData.SubscriptionStartDate.HasValue)
        sourceData["subscription_start_date"] = DateTime.SpecifyKind(importData.SubscriptionStartDate.Value, DateTimeKind.Utc);
    if (importData.PaymentStatus.HasValue)
        sourceData["payment_status"] = importData.PaymentStatus.Value.ToString();
    if (importData.NextBillingDate.HasValue)
        sourceData["next_billing_date"] = DateTime.SpecifyKind(importData.NextBillingDate.Value, DateTimeKind.Utc);
    if (importData.PaymentFailureCount.HasValue)
        sourceData["payment_failures"] = importData.PaymentFailureCount.Value;

    // Engagement data
    if (importData.LastLoginDate.HasValue)
        sourceData["last_login"] = DateTime.SpecifyKind(importData.LastLoginDate.Value, DateTimeKind.Utc);
    if (importData.WeeklyLoginFrequency.HasValue)
        sourceData["weekly_logins"] = importData.WeeklyLoginFrequency.Value;
    if (importData.FeatureUsagePercentage.HasValue)
        sourceData["feature_usage"] = importData.FeatureUsagePercentage.Value;

    // Support data
    if (importData.SupportTicketCount.HasValue)
        sourceData["total_tickets"] = importData.SupportTicketCount.Value;

    // Other fields
    if (!string.IsNullOrEmpty(importData.ExternalId))
        sourceData["external_id"] = importData.ExternalId;

    return sourceData;
}


    private ImportSummary CalculateInsights(List<Customer> customers)
    {
        if (!customers.Any())
            return ImportSummary.Empty;

        var paymentCustomers = customers.Where(c => c.PaymentDataSources.Any()).ToList();
        var averageRevenue = paymentCustomers.Any() ? paymentCustomers.Average(c => c.GetPrimaryPaymentData()?.MonthlyRecurringRevenue ?? 0) : 0;

        var customersWithSubscription = paymentCustomers.Where(c => c.GetPrimaryPaymentData()?.SubscriptionStartDate.HasValue == true).ToList();
        var averageTenure = customersWithSubscription.Any()
            ? customersWithSubscription.Average(c => (DateTime.UtcNow - (c.GetPrimaryPaymentData()?.SubscriptionStartDate ?? DateTime.UtcNow)).TotalDays / 30.0)
            : 0;

        var highRiskCustomers = customers.Count(c => c.ChurnRiskLevel >= ChurnRiskLevel.High);

        return new ImportSummary
        {
            AverageRevenue = averageRevenue,
            AverageTenureMonths = averageTenure,
            NewCustomers = customers.Count,
            HighRiskCustomers = highRiskCustomers,
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["total_imported"] = customers.Count,
                ["with_payment_data"] = paymentCustomers.Count,
                ["with_engagement_data"] = customers.Count(c => c.EngagementDataSources.Any()),
                ["average_churn_score"] = customers.Any() ? customers.Average(c => c.ChurnRiskScore) : 0
            }
        };
    }

    // Updated CSV parsing to use new DTO
    private static (List<CustomerImportData> customers, int totalRecords, List<ImportError> errors) ParseCsvFile(
      string fileContent,
      string importSource)
    {
        var customers = new List<CustomerImportData>();
        var errors = new List<ImportError>();
        var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            errors.Add(new ImportError
            {
                RowNumber = 1,
                Email = "",
                ErrorMessage = "File must contain at least a header row and one data row",
                FieldName = "File",
                ErrorTime = DateTime.UtcNow
            });
            return (customers, lines.Length, errors);
        }

        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var values = ParseCsvLine(lines[i]);

                if (values.Length != headers.Length)
                {
                    errors.Add(new ImportError
                    {
                        RowNumber = i + 1,
                        Email = "",
                        ErrorMessage = $"Row has {values.Length} columns but header has {headers.Length} columns",
                        FieldName = "General",
                        RawData = lines[i],
                        ErrorTime = DateTime.UtcNow
                    });
                    continue;
                }

                var importData = CreateImportDataFromCsvRow(headers, values, importSource, i + 1);

                // Basic validation
                var validationErrors = ValidateImportData(importData, i + 1);
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors);
                    continue;
                }

                customers.Add(importData);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError
                {
                    RowNumber = i + 1,
                    Email = "",
                    ErrorMessage = $"Error parsing row: {ex.Message}",
                    FieldName = "General",
                    RawData = lines[i],
                    ErrorTime = DateTime.UtcNow
                });
            }
        }

        return (customers, lines.Length - 1, errors); // -1 to exclude header
    }


    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = "";

        for (int i = 0; i < line.Length; i++)
        {
            var character = line[i];

            if (character == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(currentValue.Trim());
                currentValue = "";
            }
            else
            {
                currentValue += character;
            }
        }

        values.Add(currentValue.Trim());
        return values.ToArray();
    }

    private static CustomerImportData CreateImportDataFromCsvRow(string[] headers, string[] values, string importSource, int rowNumber)
    {
        var importData = new CustomerImportData
        {
            ImportSource = importSource,
            RowNumber = rowNumber
        };

        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            var header = headers[i].ToLower().Replace(" ", "").Replace("_", "");
            var value = values[i].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(value)) continue;

            try
            {
                MapFieldToImportData(importData, header, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error mapping field '{headers[i]}' with value '{value}': {ex.Message}");
            }
        }

        // Set defaults for required fields if not provided
        if (string.IsNullOrEmpty(importData.FirstName) && string.IsNullOrEmpty(importData.LastName))
        {
            importData.FirstName = "Unknown";
            importData.LastName = "Customer";
        }

        if (string.IsNullOrEmpty(importData.ExternalId))
        {
            importData.ExternalId = importData.Email ?? $"import-{rowNumber}";
        }

        return importData;
    }

    private static void MapFieldToImportData(CustomerImportData importData, string header, string value)
    {
        switch (header)
        {
            case "email":
                importData.Email = value;
                break;
            case "firstname":
            case "first":
                importData.FirstName = value;
                break;
            case "lastname":
            case "last":
                importData.LastName = value;
                break;
            case "name":
            case "fullname":
                var nameParts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                importData.FirstName = nameParts.FirstOrDefault() ?? "";
                importData.LastName = string.Join(" ", nameParts.Skip(1));
                break;
            case "phone":
            case "phonenumber":
                importData.Phone = value;
                break;
            case "company":
            case "companyname":
                importData.CompanyName = value;
                break;
            case "jobtitle":
            case "title":
                importData.JobTitle = value;
                break;
            case "plan":
            case "subscriptionplan":
                if (Enum.TryParse<SubscriptionPlan>(value, true, out var plan))
                    importData.Plan = plan;
                break;
            case "subscriptionstatus":
                if (Enum.TryParse<SubscriptionStatus>(value, true, out var subStatus))
                    importData.SubscriptionStatus = subStatus;
                break;
            case "monthlyrevenue":
            case "monthlyrecurringrevenue":
            case "mrr":
                if (decimal.TryParse(value, out var revenue))
                    importData.MonthlyRecurringRevenue = revenue;
                break;
            case "lifetimevalue":
            case "ltv":
                if (decimal.TryParse(value, out var ltv))
                    importData.LifetimeValue = ltv;
                break;
            case "subscriptionstartdate":
            case "startdate":
                if (DateTime.TryParse(value, out var startDate))
                    importData.SubscriptionStartDate = startDate;
                break;
            case "lastlogindate":
            case "lastactivity":
                if (DateTime.TryParse(value, out var lastLogin))
                    importData.LastLoginDate = lastLogin;
                break;
            case "weeklyloginfrequency":
                if (int.TryParse(value, out var frequency))
                    importData.WeeklyLoginFrequency = Math.Min(frequency, 7);
                break;
            case "featureusagepercentage":
                if (decimal.TryParse(value, out var usage))
                    importData.FeatureUsagePercentage = Math.Min(usage, 100);
                break;
            case "supportticketcount":
                if (int.TryParse(value, out var tickets))
                    importData.SupportTicketCount = tickets;
                break;
            case "age":
                if (int.TryParse(value, out var age))
                    importData.Age = age;
                break;
            case "gender":
                importData.Gender = value;
                break;
            case "location":
                importData.Location = value;
                break;
            case "country":
                importData.Country = value.Length <= 2 ? value : null;
                break;
            case "timezone":
                importData.TimeZone = value;
                break;
            case "paymentstatus":
                if (Enum.TryParse<PaymentStatus>(value, true, out var payStatus))
                    importData.PaymentStatus = payStatus;
                break;
            case "externalid":
            case "customerid":
                importData.ExternalId = value;
                break;
        }
    }

    private static List<ImportError> ValidateImportData(CustomerImportData importData, int rowNumber)
    {
        var errors = new List<ImportError>();

        if (string.IsNullOrWhiteSpace(importData.Email))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = importData.Email ?? "",
                ErrorMessage = "Email is required",
                FieldName = "Email",
                ErrorTime = DateTime.UtcNow
            });
        }
        else if (!IsValidEmail(importData.Email))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = importData.Email,
                ErrorMessage = "Invalid email format",
                FieldName = "Email",
                ErrorTime = DateTime.UtcNow
            });
        }

        if (string.IsNullOrWhiteSpace(importData.FirstName) && string.IsNullOrWhiteSpace(importData.LastName))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = importData.Email ?? "",
                ErrorMessage = "Either first name or last name is required",
                FieldName = "Name",
                ErrorTime = DateTime.UtcNow
            });
        }

        if (importData.MonthlyRecurringRevenue.HasValue && importData.MonthlyRecurringRevenue < 0)
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = importData.Email ?? "",
                ErrorMessage = "Monthly revenue cannot be negative",
                FieldName = "MonthlyRecurringRevenue",
                ErrorTime = DateTime.UtcNow
            });
        }

        return errors;
    }
    private static bool IsValidEmail(string email)
    {
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        return emailRegex.IsMatch(email);
    }
}