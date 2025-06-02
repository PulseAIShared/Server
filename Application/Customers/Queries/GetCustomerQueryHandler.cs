using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Customers;
using Domain.Customers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Queries
{
    internal sealed class GetCustomersQueryHandler(
      IApplicationDbContext context,
      IUserContext userContext)
      : IQueryHandler<GetCustomersQuery, PagedResult<CustomerResponse>>
    {
        public async Task<Result<PagedResult<CustomerResponse>>> Handle(GetCustomersQuery query, CancellationToken cancellationToken)
        {
            // Get user's company
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<PagedResult<CustomerResponse>>(UserErrors.NotFound(userContext.UserId));
            }

            var customersQuery = context.Customers
                .Where(c => c.CompanyId == user.CompanyId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.ToLower();
                customersQuery = customersQuery.Where(c =>
                    c.FirstName.ToLower().Contains(searchTerm) ||
                    c.LastName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.CompanyName != null && c.CompanyName.ToLower().Contains(searchTerm));
            }

            if (query.SubscriptionStatus.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.SubscriptionStatus == query.SubscriptionStatus.Value);
            }

            if (query.Plan.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.Plan == query.Plan.Value);
            }

            if (query.PaymentStatus.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.PaymentStatus == query.PaymentStatus.Value);
            }

            if (query.ChurnRiskLevel.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.ChurnRiskLevel == query.ChurnRiskLevel.Value);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                customersQuery = ApplySorting(customersQuery, query.SortBy, query.SortDescending);
            }
            else
            {
                // Default sorting by creation date (newest first)
                customersQuery = customersQuery.OrderByDescending(c => c.DateCreated);
            }

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

        private static IQueryable<Customer> ApplySorting(IQueryable<Customer> query, string sortBy, bool descending)
        {
            Expression<Func<Customer, object>> keySelector = sortBy.ToLower() switch
            {
                "firstname" => c => c.FirstName,
                "lastname" => c => c.LastName,
                "email" => c => c.Email,
                "company" or "companyname" => c => c.CompanyName ?? "",
                "mrr" or "monthlyrevenue" => c => c.MonthlyRecurringRevenue,
                "ltv" or "lifetimevalue" => c => c.LifetimeValue,
                "churnscore" or "churnriskscore" => c => c.ChurnRiskScore,
                "lastlogin" or "lastlogindate" => c => c.LastLoginDate ?? DateTime.MinValue,
                "subscriptionstart" => c => c.SubscriptionStartDate ?? DateTime.MinValue,
                "paymentfailures" => c => c.PaymentFailureCount,
                "created" or "datecreated" => c => c.DateCreated,
                _ => c => c.DateCreated
            };

            return descending
                ? query.OrderByDescending(keySelector)
                : query.OrderBy(keySelector);
        }
    }
}