using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Customers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Queries
{
    internal sealed class GetCustomersAtRiskQueryHandler(
      IApplicationDbContext context,
      IUserContext userContext)
      : IQueryHandler<GetCustomersAtRiskQuery, PagedResult<CustomerResponse>>
    {
        public async Task<Result<PagedResult<CustomerResponse>>> Handle(GetCustomersAtRiskQuery query, CancellationToken cancellationToken)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<PagedResult<CustomerResponse>>(UserErrors.NotFound(userContext.UserId));
            }

            var customersQuery = context.Customers
                .Where(c => c.CompanyId == user.CompanyId && c.ChurnRiskLevel >= query.MinRiskLevel)
                .OrderByDescending(c => c.ChurnRiskScore)
                .ThenByDescending(c => c.ChurnPredictionDate);

            var totalCount = await customersQuery.CountAsync(cancellationToken);

            var customers = await customersQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    JobTitle = c.JobTitle,
                    SubscriptionStatus = c.SubscriptionStatus,
                    Plan = c.Plan,
                    MonthlyRecurringRevenue = c.MonthlyRecurringRevenue,
                    LifetimeValue = c.LifetimeValue,
                    SubscriptionStartDate = c.SubscriptionStartDate,
                    SubscriptionEndDate = c.SubscriptionEndDate,
                    LastLoginDate = c.LastLoginDate,
                    WeeklyLoginFrequency = c.WeeklyLoginFrequency,
                    FeatureUsagePercentage = c.FeatureUsagePercentage,
                    SupportTicketCount = c.SupportTicketCount,
                    ChurnRiskScore = c.ChurnRiskScore,
                    ChurnRiskLevel = c.ChurnRiskLevel,
                    ChurnPredictionDate = c.ChurnPredictionDate,
                    PaymentStatus = c.PaymentStatus,
                    LastPaymentDate = c.LastPaymentDate,
                    NextBillingDate = c.NextBillingDate,
                    PaymentFailureCount = c.PaymentFailureCount,
                    Location = c.Location,
                    Country = c.Country,
                    Source = c.Source,
                    LastSyncedAt = c.LastSyncedAt,
                    DateCreated = c.DateCreated
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<CustomerResponse>
            {
                Items = customers,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
