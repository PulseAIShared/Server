using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Customers;
using Domain.Customers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Commands
{
    internal sealed class DeleteCustomersCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ILogger<DeleteCustomersCommandHandler> logger)
        : ICommandHandler<DeleteCustomersCommand, DeleteCustomersResponse>
    {
        public async Task<Result<DeleteCustomersResponse>> Handle(
            DeleteCustomersCommand command,
            CancellationToken cancellationToken)
        {
            if (!command.CustomerIds.Any())
            {
                return Result.Failure<DeleteCustomersResponse>(Error.Problem(
                    "DeleteCustomers.EmptyList",
                    "At least one customer ID must be provided"));
            }

            if (command.CustomerIds.Count > 100)
            {
                return Result.Failure<DeleteCustomersResponse>(Error.Problem(
                    "DeleteCustomers.TooMany",
                    "Cannot delete more than 100 customers at once"));
            }

            // Get user's company for authorization
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

            if (user?.CompanyId == null)
            {
                return Result.Failure<DeleteCustomersResponse>(UserErrors.UserNotInCompany);
            }

            // Check if user has permission to delete customers
            if (user.Role < User.UserRole.Admin)
            {
                return Result.Failure<DeleteCustomersResponse>(UserErrors.InsufficientPermissions);
            }

            var response = new DeleteCustomersResponse(
                TotalRequested: command.CustomerIds.Count,
                SuccessfullyDeleted: 0,
                Failed: 0,
                Errors: new List<CustomerDeletionError>()
            );

            var errors = new List<CustomerDeletionError>();
            int successCount = 0;

            // Process deletions in batches to avoid memory issues
            const int batchSize = 10;
            for (int i = 0; i < command.CustomerIds.Count; i += batchSize)
            {
                var batch = command.CustomerIds.Skip(i).Take(batchSize).ToList();

                var batchResult = await ProcessDeletionBatch(
                    batch,
                    user.CompanyId,
                    context,
                    cancellationToken);

                successCount += batchResult.SuccessCount;
                errors.AddRange(batchResult.Errors);

                // Save changes after each batch
                await context.SaveChangesAsync(cancellationToken);
            }

            var finalResponse = response with
            {
                SuccessfullyDeleted = successCount,
                Failed = errors.Count,
                Errors = errors
            };

            logger.LogInformation(
                "Customer deletion completed for user {UserId}. " +
                "Requested: {TotalRequested}, Deleted: {SuccessfullyDeleted}, Failed: {Failed}",
                userContext.UserId, finalResponse.TotalRequested,
                finalResponse.SuccessfullyDeleted, finalResponse.Failed);

            return Result.Success(finalResponse);
        }

        private async Task<BatchDeletionResult> ProcessDeletionBatch(
            List<Guid> customerIds,
            Guid companyId,
            IApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            var errors = new List<CustomerDeletionError>();
            int successCount = 0;

            // Get customers with all related data for this batch
            var customers = await context.Customers
                .Include(c => c.Activities)
                .Include(c => c.ChurnPredictions)
                .Include(c => c.CrmDataSources)
                .Include(c => c.PaymentDataSources)
                .Include(c => c.MarketingDataSources)
                .Include(c => c.SupportDataSources)
                .Include(c => c.EngagementDataSources)
                .Include(c => c.Campaigns)
                .Where(c => customerIds.Contains(c.Id) && c.CompanyId == companyId)
                .ToListAsync(cancellationToken);

            // Check for customers that don't exist or don't belong to the company
            var foundCustomerIds = customers.Select(c => c.Id).ToHashSet();
            var notFoundIds = customerIds.Except(foundCustomerIds).ToList();

            foreach (var notFoundId in notFoundIds)
            {
                errors.Add(new CustomerDeletionError(
                    notFoundId,
                    "Unknown",
                    "Customer not found or access denied"));
            }

            // Delete each customer and their related data
            foreach (var customer in customers)
            {
                try
                {
                    await DeleteCustomerAndRelatedData(customer, context, cancellationToken);
                    successCount++;

                    logger.LogDebug("Successfully deleted customer {CustomerId} ({Email})",
                        customer.Id, customer.Email);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete customer {CustomerId} ({Email})",
                        customer.Id, customer.Email);

                    errors.Add(new CustomerDeletionError(
                        customer.Id,
                        customer.Email,
                        $"Deletion failed: {ex.Message}"));
                }
            }

            return new BatchDeletionResult(successCount, errors);
        }

        private async Task DeleteCustomerAndRelatedData(
            Customer customer,
            IApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            // Delete customer activities
            if (customer.Activities.Any())
            {
                context.CustomerActivities.RemoveRange(customer.Activities);
            }

            // Delete churn predictions
            if (customer.ChurnPredictions.Any())
            {
                context.ChurnPredictions.RemoveRange(customer.ChurnPredictions);
            }

            // Delete data sources
            if (customer.CrmDataSources.Any())
            {
                context.CustomerCrmData.RemoveRange(customer.CrmDataSources);
            }

            if (customer.PaymentDataSources.Any())
            {
                context.CustomerPaymentData.RemoveRange(customer.PaymentDataSources);
            }

            if (customer.MarketingDataSources.Any())
            {
                context.CustomerMarketingData.RemoveRange(customer.MarketingDataSources);
            }

            if (customer.SupportDataSources.Any())
            {
                context.CustomerSupportData.RemoveRange(customer.SupportDataSources);
            }

            if (customer.EngagementDataSources.Any())
            {
                context.CustomerEngagementData.RemoveRange(customer.EngagementDataSources);
            }

            if (customer.Campaigns.Any())
            {
                customer.Campaigns.Clear();
            }
            context.Customers.Remove(customer);
        }

        private record BatchDeletionResult(int SuccessCount, List<CustomerDeletionError> Errors);
    }
}
