using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Import;
using Domain.Imports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Queries
{
    internal sealed class GetImportJobErrorsQueryHandler(
     IApplicationDbContext context,
     IUserContext userContext)
     : IQueryHandler<GetImportJobErrorsQuery, List<ImportErrorResponse>>
    {
        public async Task<Result<List<ImportErrorResponse>>> Handle(GetImportJobErrorsQuery query, CancellationToken cancellationToken)
        {
            var importJob = await context.ImportJobs
                .FirstOrDefaultAsync(ij => ij.Id == query.ImportJobId, cancellationToken);

            if (importJob == null)
            {
                return Result.Failure<List<ImportErrorResponse>>(ImportJobErrors.NotFound(query.ImportJobId));
            }

            if (importJob.UserId != userContext.UserId)
            {
                return Result.Failure<List<ImportErrorResponse>>(ImportJobErrors.Unauthorized());
            }

            var errors = importJob.GetValidationErrors()
                .Select(e => new ImportErrorResponse
                {
                    RowNumber = e.RowNumber,
                    Email = e.Email,
                    ErrorMessage = e.ErrorMessage,
                    FieldName = e.FieldName,
                    RawData = e.RawData,
                    ErrorTime = e.ErrorTime
                }).ToList();

            return errors;
        }
    }
}
