using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
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
    internal sealed class GetCustomerByIdQueryHandler(
     IApplicationDbContext context,
     IUserContext userContext)
     : IQueryHandler<GetCustomerByIdQuery, CustomerDetailResponse>
    {
        public async Task<Result<CustomerDetailResponse>> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<CustomerDetailResponse>(UserErrors.NotFound(userContext.UserId));
            }

            var customer = await context.Customers
                .Include(c => c.Activities.OrderByDescending(a => a.ActivityDate).Take(10))
                .Include(c => c.ChurnPredictions.OrderByDescending(cp => cp.PredictionDate).Take(5))
                .FirstOrDefaultAsync(c => c.Id == query.CustomerId && c.CompanyId == user.CompanyId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CustomerDetailResponse>(Error.NotFound(
                    "Customer.NotFound",
                    $"Customer with ID {query.CustomerId} was not found"));
            }

            var response = new CustomerDetailResponse
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                JobTitle = customer.JobTitle,
                ChurnRiskScore = customer.ChurnRiskScore,
                ChurnRiskLevel = customer.ChurnRiskLevel,
                ChurnPredictionDate = customer.ChurnPredictionDate,
                Location = customer.Location,
                Country = customer.Country,
                LastSyncedAt = customer.LastSyncedAt,
                DateCreated = customer.DateCreated,
                // Additional detail fields
                Age = customer.Age,
                Gender = customer.Gender,
                TimeZone = customer.TimeZone,
                // Recent activities
                RecentActivities = customer.Activities.Select(a => new CustomerActivityResponse
                {
                    Id = a.Id,
                    Type = a.Type,
                    Description = a.Description,
                    Metadata = a.Metadata,
                    ActivityDate = a.ActivityDate
                }).ToList(),
                // Churn prediction history
                ChurnHistory = customer.ChurnPredictions.Select(cp => new ChurnPredictionResponse
                {
                    Id = cp.Id,
                    RiskScore = cp.RiskScore,
                    RiskLevel = cp.RiskLevel,
                    PredictionDate = cp.PredictionDate,
                    RiskFactors = cp.RiskFactors,
                    ModelVersion = cp.ModelVersion
                }).ToList()
            };

            return response;
        }
    }
}
