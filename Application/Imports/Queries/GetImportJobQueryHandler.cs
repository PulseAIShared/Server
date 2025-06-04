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
    internal sealed class GetImportJobQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetImportJobQuery, ImportJobResponse>
    {
        public async Task<Result<ImportJobResponse>> Handle(GetImportJobQuery query, CancellationToken cancellationToken)
        {
            var importJob = await context.ImportJobs
                .FirstOrDefaultAsync(ij => ij.Id == query.ImportJobId, cancellationToken);

            if (importJob == null)
            {
                return Result.Failure<ImportJobResponse>(ImportJobErrors.NotFound(query.ImportJobId));
            }

            if (importJob.UserId != userContext.UserId)
            {
                return Result.Failure<ImportJobResponse>(ImportJobErrors.Unauthorized());
            }

            var response = new ImportJobResponse
            {
                Id = importJob.Id,
                FileName = importJob.FileName,
                Status = importJob.Status,
                Type = importJob.Type,
                ImportSource = importJob.ImportSource,
                TotalRecords = importJob.TotalRecords,
                ProcessedRecords = importJob.ProcessedRecords,
                SuccessfulRecords = importJob.SuccessfulRecords,
                FailedRecords = importJob.FailedRecords,
                SkippedRecords = importJob.SkippedRecords,
                UpdatedRecords = importJob.UpdatedRecords, // New
                NewRecords = importJob.NewRecords, // New
                ErrorMessage = importJob.ErrorMessage,
                CreatedAt = importJob.CreatedAt,
                StartedAt = importJob.StartedAt,
                CompletedAt = importJob.CompletedAt,
                ProgressPercentage = importJob.GetProgressPercentage(),
                ValidationErrors = importJob.GetValidationErrors()
                    .Select(e => new ImportErrorResponse
                    {
                        RowNumber = e.RowNumber,
                        Email = e.Email,
                        ErrorMessage = e.ErrorMessage,
                        FieldName = e.FieldName,
                        RawData = e.RawData,
                        ErrorTime = e.ErrorTime
                    }).ToList(),
                Updates = importJob.GetImportUpdates() // New
                    .Select(u => new ImportUpdateResponse
                    {
                        RowNumber = u.RowNumber,
                        Email = u.Email,
                        CustomerName = u.CustomerName,
                        UpdatedFields = u.UpdatedFields.Select(f => new FieldUpdateResponse
                        {
                            FieldName = f.FieldName,
                            OldValue = f.OldValue,
                            NewValue = f.NewValue
                        }).ToList(),
                        UpdateTime = u.UpdateTime
                    }).ToList()
            };

            var summary = importJob.GetImportSummary();
            if (summary != null)
            {
                response.Summary = new ImportSummaryResponse
                {
                    AverageRevenue = summary.AverageRevenue,
                    AverageTenureMonths = summary.AverageTenureMonths,
                    NewCustomers = summary.NewCustomers,
                    HighRiskCustomers = summary.HighRiskCustomers,
                    AdditionalMetrics = summary.AdditionalMetrics
                };
            }

            return response;
        }
    }

}
