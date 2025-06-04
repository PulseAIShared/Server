using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Import;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Queries
{
    internal sealed class GetUserImportHistoryQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetUserImportHistoryQuery, PagedResult<ImportJobSummaryResponse>>
    {
        public async Task<Result<PagedResult<ImportJobSummaryResponse>>> Handle(GetUserImportHistoryQuery query, CancellationToken cancellationToken)
        {
            var totalCount = await context.ImportJobs
                .Where(ij => ij.UserId == userContext.UserId)
                .CountAsync(cancellationToken);

            var importJobs = await context.ImportJobs
                .Where(ij => ij.UserId == userContext.UserId)
                .OrderByDescending(ij => ij.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ij => new ImportJobSummaryResponse
                {
                    Id = ij.Id,
                    FileName = ij.FileName,
                    Status = ij.Status,
                    Type = ij.Type,
                    TotalRecords = ij.TotalRecords,
                    SuccessfulRecords = ij.SuccessfulRecords,
                    FailedRecords = ij.FailedRecords,
                    SkippedRecords = ij.SkippedRecords,
                    UpdatedRecords = ij.UpdatedRecords, 
                    NewRecords = ij.NewRecords, 
                    CreatedAt = ij.CreatedAt,
                    CompletedAt = ij.CompletedAt,
                    ErrorMessage = ij.ErrorMessage
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ImportJobSummaryResponse>
            {
                Items = importJobs,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };

            return result;
        }
    }
}
