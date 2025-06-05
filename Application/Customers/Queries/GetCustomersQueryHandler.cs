using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Services;
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
      IUserContext userContext,
      ICustomerAggregationService customerAggregationService)
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
                // Filter by subscription status using payment data sources
                customersQuery = customersQuery.Where(c =>
                    c.PaymentDataSources.Any(p => p.IsActive && p.SubscriptionStatus == query.SubscriptionStatus.Value));
            }

            if (query.Plan.HasValue)
            {
                // Filter by plan using payment data sources
                customersQuery = customersQuery.Where(c =>
                    c.PaymentDataSources.Any(p => p.IsActive && p.Plan == query.Plan.Value));
            }

            if (query.PaymentStatus.HasValue)
            {
                // Filter by payment status using payment data sources
                customersQuery = customersQuery.Where(c =>
                    c.PaymentDataSources.Any(p => p.IsActive && p.PaymentStatus == query.PaymentStatus.Value));
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

            // Get the basic customer data with includes for performance
            var customers = await customersQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(c => c.CrmDataSources.Where(cds => cds.IsActive))
                .Include(c => c.PaymentDataSources.Where(pds => pds.IsActive))
                .Include(c => c.MarketingDataSources.Where(mds => mds.IsActive))
                .Include(c => c.SupportDataSources.Where(sds => sds.IsActive))
                .Include(c => c.EngagementDataSources.Where(eds => eds.IsActive))
                .ToListAsync(cancellationToken);

            // Convert to unified responses using the aggregation service
            var customerResponses = new List<CustomerResponse>();
            foreach (var customer in customers)
            {
                try
                {
                    var unifiedResponse = await customerAggregationService.GetUnifiedCustomerAsync(customer.Id, user.CompanyId);
                    customerResponses.Add(unifiedResponse);
                }
                catch (Exception ex)
                {
                    // Fallback to basic customer data if aggregation fails
                    var basicResponse = CreateBasicCustomerResponse(customer);
                    customerResponses.Add(basicResponse);
                }
            }

            return new PagedResult<CustomerResponse>
            {
                Items = customerResponses,
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
                "churnscore" or "churnriskscore" => c => c.ChurnRiskScore,
                "lastsynced" or "lastsyncdate" => c => c.LastSyncedAt,
                "created" or "datecreated" => c => c.DateCreated,
                // For payment-related sorting, we need to use a different approach
                "mrr" or "monthlyrevenue" => c => c.PaymentDataSources
                    .Where(p => p.IsActive && p.IsPrimarySource)
                    .Select(p => p.MonthlyRecurringRevenue)
                    .FirstOrDefault(),
                "ltv" or "lifetimevalue" => c => c.PaymentDataSources
                    .Where(p => p.IsActive && p.IsPrimarySource)
                    .Select(p => p.LifetimeValue)
                    .FirstOrDefault(),
                "lastlogin" or "lastlogindate" => c => c.EngagementDataSources
                    .Where(e => e.IsActive && e.IsPrimarySource)
                    .Select(e => e.LastLoginDate)
                    .FirstOrDefault() ?? DateTime.MinValue,
                _ => c => c.DateCreated
            };

            return descending
                ? query.OrderByDescending(keySelector)
                : query.OrderBy(keySelector);
        }

        private CustomerResponse CreateBasicCustomerResponse(Customer customer)
        {
            var response = new CustomerResponse
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                JobTitle = customer.JobTitle,
                Location = customer.Location,
                Country = customer.Country,
                ChurnRiskScore = customer.ChurnRiskScore,
                ChurnRiskLevel = customer.ChurnRiskLevel,
                ChurnPredictionDate = customer.ChurnPredictionDate,
                DateCreated = customer.DateCreated,
                LastSyncedAt = customer.LastSyncedAt,
                PrimaryCrmSource = customer.PrimaryCrmSource,
                PrimaryPaymentSource = customer.PrimaryPaymentSource,
                PrimaryMarketingSource = customer.PrimaryMarketingSource,
                PrimarySupportSource = customer.PrimarySupportSource,
                PrimaryEngagementSource = customer.PrimaryEngagementSource
            };

            // Basic source info without full aggregation
            if (customer.CrmDataSources.Any())
            {
                response.CrmInfo = customer.CrmDataSources.Take(1).Select(c => new CustomerCrmInfo
                {
                    Source = c.Source,
                    IsPrimarySource = c.IsPrimarySource,
                    LastSyncedAt = c.LastSyncedAt,
                    IsActive = c.IsActive
                }).ToList();
            }

            if (customer.PaymentDataSources.Any())
            {
                response.PaymentInfo = customer.PaymentDataSources.Take(1).Select(p => new CustomerPaymentInfo
                {
                    Source = p.Source,
                    IsPrimarySource = p.IsPrimarySource,
                    SubscriptionStatus = p.SubscriptionStatus,
                    Plan = p.Plan,
                    MonthlyRecurringRevenue = p.MonthlyRecurringRevenue,
                    LifetimeValue = p.LifetimeValue,
                    PaymentStatus = p.PaymentStatus,
                    LastSyncedAt = p.LastSyncedAt,
                    IsActive = p.IsActive
                }).ToList();
            }

            return response;
        }
    }
}