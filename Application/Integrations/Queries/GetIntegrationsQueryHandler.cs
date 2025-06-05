using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Integrations;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Integrations.Queries
{
    internal sealed class GetIntegrationsQueryHandler(
       IApplicationDbContext context,
       IUserContext userContext)
       : IQueryHandler<GetIntegrationsQuery, List<IntegrationResponse>>
    {
        public async Task<Result<List<IntegrationResponse>>> Handle(GetIntegrationsQuery query, CancellationToken cancellationToken)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);
            if (user?.CompanyId == null)
            {
                return Result.Failure<List<IntegrationResponse>>(UserErrors.UserNotInCompany);
            }

            // Start with base query for user's company
            var integrationQuery = context.Integrations
                .Where(i => i.CompanyId == user.CompanyId)
                .AsQueryable();

            // Apply filters
            if (query.Type.HasValue)
            {
                integrationQuery = integrationQuery.Where(i => i.Type == query.Type.Value);
            }

            if (query.Status.HasValue)
            {
                integrationQuery = integrationQuery.Where(i => i.Status == query.Status.Value);
            }

            // Execute query and map to response DTOs
            var integrations = await integrationQuery
                .Include(i => i.ConfiguredBy)
                .OrderByDescending(i => i.ConfiguredAt)
                .Select(i => new IntegrationResponse
                {
                    Id = i.Id.ToString(),
                    Type = i.Type.ToString(),
                    Name = i.Name,
                    Status = i.Status.ToString(),
                    LastSyncedAt = i.LastSyncedAt,
                    SyncedRecordCount = i.SyncedRecordCount,
                    ConfiguredAt = i.ConfiguredAt,
                    LastSyncError = i.LastSyncError,
                    ConfiguredBy = new IntegrationUserResponse
                    {
                        Id = i.ConfiguredBy.Id,
                        FirstName = i.ConfiguredBy.FirstName,
                        LastName = i.ConfiguredBy.LastName,
                        Email = i.ConfiguredBy.Email
                    }
                })
                .ToListAsync(cancellationToken);

            return Result.Success(integrations);
        }
    }
}
