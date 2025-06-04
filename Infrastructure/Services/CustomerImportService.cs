// Infrastructure/Services/CustomerImportService.cs - Complete implementation
using Application.Abstractions.Data;
using Application.Services;
using Domain.Customers;
using Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

namespace Infrastructure.Services;

public class CustomerImportService(
    IServiceProvider serviceProvider,
    IFileStorageService fileStorageService,
    IBackgroundJobClient backgroundJobClient,
    ILogger<CustomerImportService> logger) : ICustomerImportService
{
    private const int BATCH_SIZE = 100;
    private const int PROGRESS_UPDATE_INTERVAL = 50;

    public async Task ValidateImportFileAsync(Guid jobId, bool skipDuplicates)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            logger.LogWarning("Import job {JobId} not found for validation", jobId);
            return;
        }

        try
        {
            logger.LogInformation("Starting validation for import job {JobId} (skipDuplicates: {SkipDuplicates})",
                jobId, skipDuplicates);

            job.Status = ImportJobStatus.Validating;
            await context.SaveChangesAsync();

            // Read and parse CSV file
            var fileContent = await fileStorageService.ReadFileAsync(job.FilePath);
            logger.LogInformation("Read file content, length: {Length} characters", fileContent.Length);

            var (customers, totalRecords, parseErrors) = ParseCsvFile(fileContent, job.ImportSource!);
            logger.LogInformation("Parsed {CustomerCount} customers from {TotalRecords} records", customers.Count, totalRecords);

            job.TotalRecords = totalRecords;
            var allErrors = new List<ImportError>(parseErrors);

            // Check for duplicates based on skipDuplicates setting
            var duplicateInfo = await AnalyzeDuplicatesAsync(customers, job.CompanyId, context, skipDuplicates);

            if (skipDuplicates)
            {
                logger.LogInformation("Skip duplicates enabled: {SkippedCount} existing customers will be skipped",
                    duplicateInfo.SkippedEmails.Count);
            }
            else
            {
                logger.LogInformation("Skip duplicates disabled: {UpdateCount} existing customers will be updated",
                    duplicateInfo.ExistingEmails.Count);
            }

            // Store validation errors (only actual parsing/validation errors, not duplicates)
            if (allErrors.Count > 0)
            {
                job.SetValidationErrors(allErrors);
                logger.LogWarning("Import job {JobId} has {ErrorCount} validation errors", jobId, allErrors.Count);
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
                logger.LogInformation("Validation successful for job {JobId}, auto-queuing processing", jobId);

                var processingJobId = backgroundJobClient.Enqueue<IImportBackgroundService>(
                    "imports",
                    service => service.ProcessImportAsync(jobId));

                logger.LogInformation("Auto-queued processing job {HangfireJobId} for import {ImportJobId}",
                    processingJobId, jobId);
            }
            else
            {
                logger.LogWarning("Import job {JobId} has critical errors, skipping auto-processing", jobId);
            }

            logger.LogInformation("Validation completed for import job {JobId}. Total: {TotalRecords}, Errors: {ErrorCount}",
                jobId, totalRecords, allErrors.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate import file for job {JobId}", jobId);
            job.Fail($"Validation failed: {ex.Message}");
            await context.SaveChangesAsync();
            throw;
        }
    }

    public async Task ProcessImportFileAsync(Guid jobId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            logger.LogWarning("Import job {JobId} not found for processing", jobId);
            return;
        }

        try
        {
            logger.LogInformation("Starting processing for import job {JobId}", jobId);

            job.Start();
            job.Raise(new ImportJobStartedDomainEvent(
                job.Id, job.UserId, job.FileName, job.Type, job.ImportSource));

            await context.SaveChangesAsync();

            // Determine skipDuplicates from the job (stored during creation)
            bool skipDuplicates = job.ShouldSkipDuplicates();

            // Read and parse CSV file again
            var fileContent = await fileStorageService.ReadFileAsync(job.FilePath);
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

            logger.LogInformation("Processing {TotalCustomers} customers, {ExistingCount} already exist (skipDuplicates: {SkipDuplicates})",
                customers.Count, existingCustomers.Count, skipDuplicates);

            // Process in batches
            for (int i = 0; i < customers.Count; i += BATCH_SIZE)
            {
                var batch = customers.Skip(i).Take(BATCH_SIZE).ToList();
                await ProcessBatchWithDuplicateHandling(batch, job, result, existingCustomers, i, context, skipDuplicates);

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

                    logger.LogDebug("Import job {JobId} progress: {ProcessedRecords}/{TotalRecords} ({ProgressPercentage:F1}%) - New: {NewRecords}, Updated: {UpdatedRecords}, Skipped: {SkippedRecords}",
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
                await fileStorageService.DeleteFileAsync(job.FilePath);
                logger.LogInformation("Cleaned up import file {FilePath} for job {JobId}", job.FilePath, jobId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete import file {FilePath} for job {JobId}", job.FilePath, jobId);
            }

            logger.LogInformation("Import job {JobId} completed successfully. " +
                "{NewRecords} new records, {UpdatedRecords} updated, {FailedRecords} failed, {SkippedRecords} skipped",
                jobId, result.NewRecords, result.UpdatedRecords, result.FailedRecords, result.SkippedRecords);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process import file for job {JobId}", jobId);

            job.Fail($"Import failed: {ex.Message}");
            job.Raise(new ImportJobCompletedDomainEvent(
                job.Id, job.UserId, ImportJobStatus.Failed,
                job.TotalRecords, job.SuccessfulRecords, job.FailedRecords, job.SkippedRecords,
                ex.Message, null));

            await context.SaveChangesAsync();
            throw;
        }
    }

    public async Task CancelImportJobAsync(Guid jobId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            logger.LogWarning("Import job {JobId} not found for cancellation", jobId);
            return;
        }

        if (!job.CanBeCancelled)
        {
            logger.LogWarning("Import job {JobId} cannot be cancelled in status {Status}", jobId, job.Status);
            return;
        }

        try
        {
            job.Cancel();
            await context.SaveChangesAsync();

            try
            {
                await fileStorageService.DeleteFileAsync(job.FilePath);
                logger.LogInformation("Cleaned up file {FilePath} for cancelled job {JobId}", job.FilePath, jobId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete file {FilePath} for cancelled job {JobId}", job.FilePath, jobId);
            }

            logger.LogInformation("Cancelled import job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel import job {JobId}", jobId);
            throw;
        }
    }

    public async Task RetryImportJobAsync(Guid jobId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var job = await context.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            logger.LogWarning("Import job {JobId} not found for retry", jobId);
            return;
        }

        if (job.Status != ImportJobStatus.Failed)
        {
            logger.LogWarning("Cannot retry import job {JobId} in status {Status}", jobId, job.Status);
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

            logger.LogInformation("Reset import job {JobId} for retry", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retry import job {JobId}", jobId);
            throw;
        }
    }

    public async Task CleanupOldImportJobsAsync(int olderThanDays = 30)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
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
                    if (await fileStorageService.FileExistsAsync(job.FilePath))
                    {
                        await fileStorageService.DeleteFileAsync(job.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete file {FilePath} for job {JobId}", job.FilePath, job.Id);
                }
            }

            context.ImportJobs.RemoveRange(oldJobs);
            await context.SaveChangesAsync();

            logger.LogInformation("Cleaned up {Count} old import jobs older than {Days} days", oldJobs.Count, olderThanDays);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cleanup old import jobs");
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
        List<Customer> customers,
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
        List<Customer> batch,
        ImportJob job,
        ImportJobResult result,
        Dictionary<string, Customer> existingCustomers,
        int batchStartIndex,
        IApplicationDbContext context,
        bool skipDuplicates)
    {
        foreach (var (customer, index) in batch.Select((c, i) => (c, i)))
        {
            try
            {
                var emailKey = customer.Email.ToLower();
                var rowNumber = batchStartIndex + index + 2; // +2 for header and 1-based indexing

                if (existingCustomers.TryGetValue(emailKey, out var existingCustomer))
                {
                    if (skipDuplicates)
                    {
                        // Skip this customer entirely
                        result.SkippedRecords++;
                        logger.LogDebug("Skipped existing customer {Email} at row {RowNumber} in job {JobId}",
                            customer.Email, rowNumber, job.Id);
                    }
                    else
                    {
                        // Update existing customer and track changes
                        var updates = UpdateCustomerFieldsWithTracking(existingCustomer, customer);

                        if (updates.UpdatedFields.Any())
                        {
                            updates.RowNumber = rowNumber;
                            updates.Email = customer.Email;
                            updates.CustomerName = $"{customer.FirstName} {customer.LastName}".Trim();
                            result.Updates.Add(updates);
                            result.UpdatedRecords++;

                            logger.LogDebug("Updated existing customer {Email} with {FieldCount} changes in job {JobId}",
                                customer.Email, updates.UpdatedFields.Count, job.Id);
                        }
                        else
                        {
                            logger.LogDebug("No changes detected for existing customer {Email} in job {JobId}",
                                customer.Email, job.Id);
                        }

                        result.SuccessfulRecords++;
                    }
                }
                else
                {
                    // New customer
                    customer.CompanyId = job.CompanyId;
                    customer.LastSyncedAt = DateTime.UtcNow;
                    customer.ExternalId = customer.ExternalId ?? customer.Email;
                    customer.Source = job.ImportSource ?? "import";

                    SetCustomerDefaults(customer);

                    context.Customers.Add(customer);
                    result.NewRecords++;
                    result.SuccessfulRecords++;

                    logger.LogDebug("Added new customer {Email} at row {RowNumber} in job {JobId}",
                        customer.Email, rowNumber, job.Id);
                }

                result.ProcessedRecords++;
            }
            catch (Exception ex)
            {
                result.FailedRecords++;
                var error = new ImportError
                {
                    RowNumber = batchStartIndex + index + 2,
                    Email = customer.Email,
                    ErrorMessage = ex.Message,
                    FieldName = "General",
                    RawData = $"FirstName: {customer.FirstName}, LastName: {customer.LastName}, Email: {customer.Email}"
                };

                result.Errors.Add(error);

                logger.LogWarning(ex, "Failed to import customer {Email} at row {RowNumber} in job {JobId}",
                    customer.Email, error.RowNumber, job.Id);
            }
        }

        await context.SaveChangesAsync();
    }

    private static ImportUpdate UpdateCustomerFieldsWithTracking(Customer existing, Customer updated)
    {
        var importUpdate = new ImportUpdate();
        var updatedFields = new List<FieldUpdate>();

        // Track all potential field updates
        CheckAndTrackFieldUpdate(updatedFields, "FirstName", existing.FirstName, updated.FirstName);
        CheckAndTrackFieldUpdate(updatedFields, "LastName", existing.LastName, updated.LastName);
        CheckAndTrackFieldUpdate(updatedFields, "Phone", existing.Phone, updated.Phone);
        CheckAndTrackFieldUpdate(updatedFields, "CompanyName", existing.CompanyName, updated.CompanyName);
        CheckAndTrackFieldUpdate(updatedFields, "JobTitle", existing.JobTitle, updated.JobTitle);
        CheckAndTrackFieldUpdate(updatedFields, "Plan", existing.Plan.ToString(), updated.Plan.ToString());
        CheckAndTrackFieldUpdate(updatedFields, "SubscriptionStatus", existing.SubscriptionStatus.ToString(), updated.SubscriptionStatus.ToString());
        CheckAndTrackFieldUpdate(updatedFields, "MonthlyRecurringRevenue", existing.MonthlyRecurringRevenue.ToString("F2"), updated.MonthlyRecurringRevenue.ToString("F2"));
        CheckAndTrackFieldUpdate(updatedFields, "LifetimeValue", existing.LifetimeValue.ToString("F2"), updated.LifetimeValue.ToString("F2"));
        CheckAndTrackFieldUpdate(updatedFields, "SubscriptionStartDate", existing.SubscriptionStartDate?.ToString("yyyy-MM-dd"), updated.SubscriptionStartDate?.ToString("yyyy-MM-dd"));
        CheckAndTrackFieldUpdate(updatedFields, "SubscriptionEndDate", existing.SubscriptionEndDate?.ToString("yyyy-MM-dd"), updated.SubscriptionEndDate?.ToString("yyyy-MM-dd"));
        CheckAndTrackFieldUpdate(updatedFields, "LastLoginDate", existing.LastLoginDate?.ToString("yyyy-MM-dd"), updated.LastLoginDate?.ToString("yyyy-MM-dd"));
        CheckAndTrackFieldUpdate(updatedFields, "WeeklyLoginFrequency", existing.WeeklyLoginFrequency.ToString(), updated.WeeklyLoginFrequency.ToString());
        CheckAndTrackFieldUpdate(updatedFields, "FeatureUsagePercentage", existing.FeatureUsagePercentage.ToString("F2"), updated.FeatureUsagePercentage.ToString("F2"));
        CheckAndTrackFieldUpdate(updatedFields, "SupportTicketCount", existing.SupportTicketCount.ToString(), updated.SupportTicketCount.ToString());
        CheckAndTrackFieldUpdate(updatedFields, "Age", existing.Age?.ToString(), updated.Age?.ToString());
        CheckAndTrackFieldUpdate(updatedFields, "Gender", existing.Gender, updated.Gender);
        CheckAndTrackFieldUpdate(updatedFields, "Location", existing.Location, updated.Location);
        CheckAndTrackFieldUpdate(updatedFields, "Country", existing.Country, updated.Country);
        CheckAndTrackFieldUpdate(updatedFields, "TimeZone", existing.TimeZone, updated.TimeZone);
        CheckAndTrackFieldUpdate(updatedFields, "PaymentStatus", existing.PaymentStatus.ToString(), updated.PaymentStatus.ToString());
        CheckAndTrackFieldUpdate(updatedFields, "LastPaymentDate", existing.LastPaymentDate?.ToString("yyyy-MM-dd"), updated.LastPaymentDate?.ToString("yyyy-MM-dd"));
        CheckAndTrackFieldUpdate(updatedFields, "NextBillingDate", existing.NextBillingDate?.ToString("yyyy-MM-dd"), updated.NextBillingDate?.ToString("yyyy-MM-dd"));

        // Only update fields that actually changed
        foreach (var fieldUpdate in updatedFields)
        {
            switch (fieldUpdate.FieldName)
            {
                case "FirstName": existing.FirstName = updated.FirstName; break;
                case "LastName": existing.LastName = updated.LastName; break;
                case "Phone": existing.Phone = updated.Phone; break;
                case "CompanyName": existing.CompanyName = updated.CompanyName; break;
                case "JobTitle": existing.JobTitle = updated.JobTitle; break;
                case "Plan": existing.Plan = updated.Plan; break;
                case "SubscriptionStatus": existing.SubscriptionStatus = updated.SubscriptionStatus; break;
                case "MonthlyRecurringRevenue": existing.MonthlyRecurringRevenue = updated.MonthlyRecurringRevenue; break;
                case "LifetimeValue": existing.LifetimeValue = updated.LifetimeValue; break;
                case "SubscriptionStartDate": existing.SubscriptionStartDate = updated.SubscriptionStartDate; break;
                case "SubscriptionEndDate": existing.SubscriptionEndDate = updated.SubscriptionEndDate; break;
                case "LastLoginDate": existing.LastLoginDate = updated.LastLoginDate; break;
                case "WeeklyLoginFrequency": existing.WeeklyLoginFrequency = updated.WeeklyLoginFrequency; break;
                case "FeatureUsagePercentage": existing.FeatureUsagePercentage = updated.FeatureUsagePercentage; break;
                case "SupportTicketCount": existing.SupportTicketCount = updated.SupportTicketCount; break;
                case "Age": existing.Age = updated.Age; break;
                case "Gender": existing.Gender = updated.Gender; break;
                case "Location": existing.Location = updated.Location; break;
                case "Country": existing.Country = updated.Country; break;
                case "TimeZone": existing.TimeZone = updated.TimeZone; break;
                case "PaymentStatus": existing.PaymentStatus = updated.PaymentStatus; break;
                case "LastPaymentDate": existing.LastPaymentDate = updated.LastPaymentDate; break;
                case "NextBillingDate": existing.NextBillingDate = updated.NextBillingDate; break;
            }
        }

        // Always update sync metadata if any changes were made
        if (updatedFields.Any())
        {
            existing.LastSyncedAt = DateTime.UtcNow;
        }

        importUpdate.UpdatedFields = updatedFields;
        return importUpdate;
    }

    private static void CheckAndTrackFieldUpdate(List<FieldUpdate> updates, string fieldName, string? oldValue, string? newValue)
    {
        var oldVal = oldValue ?? "";
        var newVal = newValue ?? "";

        if (!oldVal.Equals(newVal, StringComparison.OrdinalIgnoreCase))
        {
            updates.Add(new FieldUpdate
            {
                FieldName = fieldName,
                OldValue = oldVal,
                NewValue = newVal
            });
        }
    }

    private static void SetCustomerDefaults(Customer customer)
    {
        if (customer.ChurnRiskLevel == default)
            customer.ChurnRiskLevel = ChurnRiskLevel.Low;

        if (customer.PaymentStatus == default)
            customer.PaymentStatus = PaymentStatus.Active;

        if (customer.SubscriptionStatus == default)
            customer.SubscriptionStatus = SubscriptionStatus.Active;
    }

    private static (List<Customer> customers, int totalRecords, List<ImportError> errors) ParseCsvFile(
        string fileContent,
        string importSource)
    {
        var customers = new List<Customer>();
        var errors = new List<ImportError>();
        var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            errors.Add(new ImportError
            {
                RowNumber = 1,
                Email = "",
                ErrorMessage = "File must contain at least a header row and one data row",
                FieldName = "File"
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
                        RawData = lines[i]
                    });
                    continue;
                }

                var customer = CreateCustomerFromCsvRow(headers, values, importSource, i + 1);

                // Basic validation
                var validationErrors = ValidateCustomer(customer, i + 1);
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors);
                    continue;
                }

                customers.Add(customer);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError
                {
                    RowNumber = i + 1,
                    Email = "",
                    ErrorMessage = $"Error parsing row: {ex.Message}",
                    FieldName = "General",
                    RawData = lines[i]
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

    private static Customer CreateCustomerFromCsvRow(string[] headers, string[] values, string importSource, int rowNumber)
    {
        var customer = new Customer
        {
            Source = importSource,
            ChurnRiskLevel = ChurnRiskLevel.Low,
            ChurnRiskScore = 0,
            PaymentStatus = PaymentStatus.Active,
            SubscriptionStatus = SubscriptionStatus.Active,
            Plan = SubscriptionPlan.Basic
        };

        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            var header = headers[i].ToLower().Replace(" ", "").Replace("_", "");
            var value = values[i].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(value)) continue;

            try
            {
                MapFieldToCustomer(customer, header, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error mapping field '{headers[i]}' with value '{value}': {ex.Message}");
            }
        }

        // Set defaults for required fields if not provided
        if (string.IsNullOrEmpty(customer.FirstName) && string.IsNullOrEmpty(customer.LastName))
        {
            customer.FirstName = "Unknown";
            customer.LastName = "Customer";
        }

        if (string.IsNullOrEmpty(customer.ExternalId))
        {
            customer.ExternalId = customer.Email ?? $"import-{rowNumber}";
        }

        return customer;
    }

    private static void MapFieldToCustomer(Customer customer, string header, string value)
    {
        switch (header)
        {
            case "email":
                customer.Email = value;
                break;
            case "firstname":
            case "first":
                customer.FirstName = value;
                break;
            case "lastname":
            case "last":
                customer.LastName = value;
                break;
            case "name":
            case "fullname":
                var nameParts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                customer.FirstName = nameParts.FirstOrDefault() ?? "";
                customer.LastName = string.Join(" ", nameParts.Skip(1));
                break;
            case "phone":
            case "phonenumber":
                customer.Phone = value;
                break;
            case "company":
            case "companyname":
                customer.CompanyName = value;
                break;
            case "jobtitle":
            case "title":
                customer.JobTitle = value;
                break;
            case "plan":
            case "subscriptionplan":
                if (Enum.TryParse<SubscriptionPlan>(value, true, out var plan))
                    customer.Plan = plan;
                break;
            case "subscriptionstatus":
                if (Enum.TryParse<SubscriptionStatus>(value, true, out var subStatus))
                    customer.SubscriptionStatus = subStatus;
                break;
            case "monthlyrevenue":
            case "monthlyrecurringrevenue":
            case "mrr":
                if (decimal.TryParse(value, out var revenue))
                    customer.MonthlyRecurringRevenue = revenue;
                break;
            case "lifetimevalue":
            case "ltv":
                if (decimal.TryParse(value, out var ltv))
                    customer.LifetimeValue = ltv;
                break;
            case "subscriptionstartdate":
            case "startdate":
                if (DateTime.TryParse(value, out var startDate))
                    customer.SubscriptionStartDate = startDate;
                break;
            case "subscriptionenddate":
            case "enddate":
                if (DateTime.TryParse(value, out var endDate))
                    customer.SubscriptionEndDate = endDate;
                break;
            case "lastlogindate":
            case "lastactivity":
                if (DateTime.TryParse(value, out var lastLogin))
                    customer.LastLoginDate = lastLogin;
                break;
            case "weeklyloginfrequency":
                if (int.TryParse(value, out var frequency))
                    customer.WeeklyLoginFrequency = Math.Min(frequency, 7);
                break;
            case "featureusagepercentage":
                if (decimal.TryParse(value, out var usage))
                    customer.FeatureUsagePercentage = Math.Min(usage, 100);
                break;
            case "supportticketcount":
                if (int.TryParse(value, out var tickets))
                    customer.SupportTicketCount = tickets;
                break;
            case "age":
                if (int.TryParse(value, out var age))
                    customer.Age = age;
                break;
            case "gender":
                customer.Gender = value;
                break;
            case "location":
                customer.Location = value;
                break;
            case "country":
                customer.Country = value.Length <= 2 ? value : null;
                break;
            case "timezone":
                customer.TimeZone = value;
                break;
            case "paymentstatus":
                if (Enum.TryParse<PaymentStatus>(value, true, out var payStatus))
                    customer.PaymentStatus = payStatus;
                break;
            case "lastpaymentdate":
                if (DateTime.TryParse(value, out var lastPayment))
                    customer.LastPaymentDate = lastPayment;
                break;
            case "nextbillingdate":
                if (DateTime.TryParse(value, out var nextBilling))
                    customer.NextBillingDate = nextBilling;
                break;
            case "externalid":
            case "customerid":
                customer.ExternalId = value;
                break;
        }
    }

    private static List<ImportError> ValidateCustomer(Customer customer, int rowNumber)
    {
        var errors = new List<ImportError>();

        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = customer.Email ?? "",
                ErrorMessage = "Email is required",
                FieldName = "Email"
            });
        }
        else if (!IsValidEmail(customer.Email))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = customer.Email,
                ErrorMessage = "Invalid email format",
                FieldName = "Email"
            });
        }

        if (string.IsNullOrWhiteSpace(customer.FirstName) && string.IsNullOrWhiteSpace(customer.LastName))
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = customer.Email ?? "",
                ErrorMessage = "Either first name or last name is required",
                FieldName = "Name"
            });
        }

        if (customer.MonthlyRecurringRevenue < 0)
        {
            errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                Email = customer.Email ?? "",
                ErrorMessage = "Monthly revenue cannot be negative",
                FieldName = "MonthlyRecurringRevenue"
            });
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static ImportSummary CalculateInsights(List<Customer> customers)
    {
        if (customers.Count == 0)
            return new ImportSummary();

        var now = DateTime.UtcNow;
        var threeMonthsAgo = now.AddMonths(-3);

        var summary = new ImportSummary
        {
            AverageRevenue = customers.Any() ? customers.Average(c => c.MonthlyRecurringRevenue) : 0,
            NewCustomers = customers.Count(c =>
                c.SubscriptionStartDate.HasValue &&
                c.SubscriptionStartDate.Value >= threeMonthsAgo),
            HighRiskCustomers = customers.Count(c => c.ChurnRiskLevel == ChurnRiskLevel.High)
        };

        // Calculate average tenure for customers with subscription start dates
        var customersWithTenure = customers.Where(c => c.SubscriptionStartDate.HasValue).ToList();
        if (customersWithTenure.Any())
        {
            summary.AverageTenureMonths = customersWithTenure
                .Average(c => (now - c.SubscriptionStartDate!.Value).TotalDays / 30.44);
        }

        // Add additional metrics
        summary.AdditionalMetrics = new Dictionary<string, object>
        {
            ["averageLifetimeValue"] = customers.Any() ? customers.Average(c => c.LifetimeValue) : 0,
            ["activeSubscriptions"] = customers.Count(c => c.SubscriptionStatus == SubscriptionStatus.Active),
            ["trialCustomers"] = customers.Count(c => c.SubscriptionStatus == SubscriptionStatus.Trial),
            ["averageFeatureUsage"] = customers.Any() ? customers.Average(c => c.FeatureUsagePercentage) : 0,
            ["customersByPlan"] = customers.GroupBy(c => c.Plan).ToDictionary(g => g.Key.ToString(), g => g.Count())
        };

        return summary;
    }

    // Helper class
    private class DuplicateAnalysisResult
    {
        public HashSet<string> ExistingEmails { get; set; } = new();
        public HashSet<string> SkippedEmails { get; set; } = new();
    }
}