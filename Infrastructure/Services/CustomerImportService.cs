using Application.Abstractions.Data;
using Application.Services;
using Domain.Customers;
using Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Enums;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public class CustomerImportService(
    IServiceProvider serviceProvider, // Keep serviceProvider for creating scopes
    IFileStorageService fileStorageService,
    ILogger<CustomerImportService> logger) : ICustomerImportService
{
    private const int BATCH_SIZE = 100;
    private const int PROGRESS_UPDATE_INTERVAL = 50;

    public async Task ValidateImportFileAsync(Guid jobId, bool skipDuplicates)
    {
        // Create a new scope for this operation
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
            logger.LogInformation("Starting validation for import job {JobId}", jobId);

            job.Status = ImportJobStatus.Validating;
            await context.SaveChangesAsync();

            // Read and parse CSV file
            var fileContent = await fileStorageService.ReadFileAsync(job.FilePath);
            logger.LogInformation("Read file content, length: {Length} characters", fileContent.Length);

            var (customers, totalRecords, parseErrors) = ParseCsvFile(fileContent, job.ImportSource!);
            logger.LogInformation("Parsed {CustomerCount} customers from {TotalRecords} records", customers.Count, totalRecords);

            job.TotalRecords = totalRecords;
            var allErrors = new List<ImportError>(parseErrors);

            // Check for duplicates if not skipping
            if (!skipDuplicates)
            {
                logger.LogInformation("Checking for duplicate customers...");
                var duplicateErrors = await CheckForDuplicatesAsync(customers, job.CompanyId, context);
                allErrors.AddRange(duplicateErrors);
                logger.LogInformation("Found {DuplicateCount} duplicate customers", duplicateErrors.Count);
            }

            // Store validation errors
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

            logger.LogInformation("Validation completed for import job {JobId}. Total: {TotalRecords}, Errors: {ErrorCount}",
                jobId, totalRecords, allErrors.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate import file for job {JobId}", jobId);
            job.Fail($"Validation failed: {ex.Message}");
            await context.SaveChangesAsync();
            throw; // Re-throw for Hangfire retry handling
        }
    }

    public async Task ProcessImportFileAsync(Guid jobId)
    {
        // Create a new scope for this operation
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

            // Raise job started event
            job.Raise(new ImportJobStartedDomainEvent(
                job.Id,
                job.UserId,
                job.FileName,
                job.Type,
                job.ImportSource
            ));

            await context.SaveChangesAsync();

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
                Errors = new List<ImportError>()
            };

            // Get existing customers for duplicate checking
            var emails = customers.Select(c => c.Email.ToLower()).ToList();
            var existingCustomers = await context.Customers
                .Where(c => c.CompanyId == job.CompanyId && emails.Contains(c.Email.ToLower()))
                .ToDictionaryAsync(c => c.Email.ToLower(), c => c);

            logger.LogInformation("Processing {TotalCustomers} customers, {ExistingCount} already exist",
                customers.Count, existingCustomers.Count);

            // Process in batches to avoid memory issues
            for (int i = 0; i < customers.Count; i += BATCH_SIZE)
            {
                var batch = customers.Skip(i).Take(BATCH_SIZE).ToList();
                await ProcessBatch(batch, job, result, existingCustomers, i, context);

                // Update progress periodically
                if (i % PROGRESS_UPDATE_INTERVAL == 0 || i + BATCH_SIZE >= customers.Count)
                {
                    job.UpdateProgress(
                        Math.Min(i + BATCH_SIZE, customers.Count),
                        result.SuccessfulRecords,
                        result.FailedRecords,
                        result.SkippedRecords
                    );

                    // Raise progress event
                    job.Raise(new ImportJobProgressDomainEvent(
                        job.Id,
                        job.UserId,
                        job.ProcessedRecords,
                        job.TotalRecords,
                        job.GetProgressPercentage()
                    ));

                    await context.SaveChangesAsync();

                    logger.LogDebug("Import job {JobId} progress: {ProcessedRecords}/{TotalRecords} ({ProgressPercentage:F1}%)",
                        jobId, job.ProcessedRecords, job.TotalRecords, job.GetProgressPercentage());
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
                job.Id,
                job.UserId,
                ImportJobStatus.Completed,
                result.TotalRecords,
                result.SuccessfulRecords,
                result.FailedRecords,
                result.SkippedRecords,
                null,
                new ImportSummary(
                    result.Summary.AverageRevenue,
                    result.Summary.AverageTenureMonths,
                    result.Summary.NewCustomers,
                    result.Summary.HighRiskCustomers
                )
            ));

            await context.SaveChangesAsync();

            // Clean up file after successful import
            try
            {
                await fileStorageService.DeleteFileAsync(job.FilePath);
                logger.LogInformation("Cleaned up import file {FilePath} for job {JobId}", job.FilePath, jobId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete import file {FilePath} for job {JobId}", job.FilePath, jobId);
            }

            logger.LogInformation("Import job {JobId} completed successfully. {SuccessfulRecords} records imported, {FailedRecords} failed, {SkippedRecords} skipped",
                jobId, result.SuccessfulRecords, result.FailedRecords, result.SkippedRecords);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process import file for job {JobId}", jobId);

            job.Fail($"Import failed: {ex.Message}");

            // Raise domain event for failed import
            job.Raise(new ImportJobCompletedDomainEvent(
                job.Id,
                job.UserId,
                ImportJobStatus.Failed,
                job.TotalRecords,
                job.SuccessfulRecords,
                job.FailedRecords,
                job.SkippedRecords,
                ex.Message,
                null
            ));

            await context.SaveChangesAsync();
            throw; // Re-throw for Hangfire retry handling
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

            // Clean up file
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
            job.ProcessedRecords = 0;
            job.SuccessfulRecords = 0;
            job.FailedRecords = 0;
            job.SkippedRecords = 0;
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
                    // Delete associated file
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

            // Remove jobs from database
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

    // Private helper methods
    private async Task<List<ImportError>> CheckForDuplicatesAsync(List<Customer> customers, Guid companyId, IApplicationDbContext context)
    {
        var errors = new List<ImportError>();
        var emails = customers.Select(c => c.Email.ToLower()).ToList();

        var existingEmails = await context.Customers
            .Where(c => c.CompanyId == companyId && emails.Contains(c.Email.ToLower()))
            .Select(c => c.Email.ToLower())
            .ToHashSetAsync();

        for (int i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];
            if (existingEmails.Contains(customer.Email.ToLower()))
            {
                errors.Add(new ImportError
                {
                    RowNumber = i + 2,
                    Email = customer.Email,
                    ErrorMessage = "Customer with this email already exists",
                    FieldName = "Email"
                });
            }
        }

        return errors;
    }

    private async Task ProcessBatch(
        List<Customer> batch,
        ImportJob job,
        ImportJobResult result,
        Dictionary<string, Customer> existingCustomers,
        int batchStartIndex,
        IApplicationDbContext context)
    {
        foreach (var (customer, index) in batch.Select((c, i) => (c, i)))
        {
            try
            {
                var emailKey = customer.Email.ToLower();

                if (existingCustomers.TryGetValue(emailKey, out var existingCustomer))
                {
                    UpdateCustomerFields(existingCustomer, customer);
                    result.SuccessfulRecords++;
                    logger.LogDebug("Updated existing customer {Email} in job {JobId}", customer.Email, job.Id);
                }
                else
                {
                    customer.CompanyId = job.CompanyId;
                    customer.LastSyncedAt = DateTime.UtcNow;
                    customer.ExternalId = customer.ExternalId ?? customer.Email;
                    customer.Source = job.ImportSource ?? "import";

                    if (customer.ChurnRiskLevel == default)
                        customer.ChurnRiskLevel = ChurnRiskLevel.Low;

                    if (customer.PaymentStatus == default)
                        customer.PaymentStatus = PaymentStatus.Active;

                    if (customer.SubscriptionStatus == default)
                        customer.SubscriptionStatus = SubscriptionStatus.Active;

                    context.Customers.Add(customer);
                    result.SuccessfulRecords++;
                    logger.LogDebug("Added new customer {Email} in job {JobId}", customer.Email, job.Id);
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

    // Add all the remaining helper methods from your original service...
    // (ParseCsvLine, CreateCustomerFromCsvRow, MapFieldToCustomer, ValidateCustomer, etc.)
    // These remain the same as in your original implementation

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

    // Add the rest of the helper methods (MapFieldToCustomer, ValidateCustomer, etc.)
    // These are exactly the same as in your original implementation

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

    private static void UpdateCustomerFields(Customer existing, Customer updated)
    {
        existing.FirstName = updated.FirstName;
        existing.LastName = updated.LastName;
        existing.Phone = updated.Phone;
        existing.CompanyName = updated.CompanyName;
        existing.JobTitle = updated.JobTitle;
        existing.Plan = updated.Plan;
        existing.SubscriptionStatus = updated.SubscriptionStatus;
        existing.MonthlyRecurringRevenue = updated.MonthlyRecurringRevenue;
        existing.LifetimeValue = updated.LifetimeValue;
        existing.SubscriptionStartDate = updated.SubscriptionStartDate;
        existing.SubscriptionEndDate = updated.SubscriptionEndDate;
        existing.LastLoginDate = updated.LastLoginDate;
        existing.WeeklyLoginFrequency = updated.WeeklyLoginFrequency;
        existing.FeatureUsagePercentage = updated.FeatureUsagePercentage;
        existing.SupportTicketCount = updated.SupportTicketCount;
        existing.Age = updated.Age;
        existing.Gender = updated.Gender;
        existing.Location = updated.Location;
        existing.Country = updated.Country;
        existing.TimeZone = updated.TimeZone;
        existing.PaymentStatus = updated.PaymentStatus;
        existing.LastPaymentDate = updated.LastPaymentDate;
        existing.NextBillingDate = updated.NextBillingDate;
        existing.LastSyncedAt = DateTime.UtcNow;
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
}