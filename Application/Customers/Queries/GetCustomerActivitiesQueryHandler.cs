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
    internal sealed class GetCustomerActivitiesQueryHandler(
       IApplicationDbContext context,
       IUserContext userContext)
       : IQueryHandler<GetCustomerActivitiesQuery, PagedResult<CustomerActivityResponse>>
    {
        public async Task<Result<PagedResult<CustomerActivityResponse>>> Handle(GetCustomerActivitiesQuery query, CancellationToken cancellationToken)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<PagedResult<CustomerActivityResponse>>(UserErrors.NotFound(userContext.UserId));
            }

            // First verify the customer belongs to the user's company
            var customerExists = await context.Customers
                .AnyAsync(c => c.Id == query.CustomerId && c.CompanyId == user.CompanyId, cancellationToken);

            if (!customerExists)
            {
                return Result.Failure<PagedResult<CustomerActivityResponse>>(Error.NotFound(
                    "Customer.NotFound",
                    $"Customer with ID {query.CustomerId} was not found"));
            }

            var activitiesQuery = context.CustomerActivities
                .Where(a => a.CustomerId == query.CustomerId)
                .OrderByDescending(a => a.ActivityDate);

            var totalCount = await activitiesQuery.CountAsync(cancellationToken);

            var activities = await activitiesQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => new CustomerActivityResponse
                {
                    Id = a.Id,
                    Type = a.Type,
                    Description = a.Description,
                    Metadata = a.Metadata,
                    ActivityDate = a.ActivityDate
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<CustomerActivityResponse>
            {
                Items = activities,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
    }
}
