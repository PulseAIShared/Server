using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public sealed record ImportJobCompletedDomainEvent(
        Guid ImportJobId,
        Guid UserId,
        ImportJobStatus Status,
        int TotalRecords,
        int SuccessfulRecords,
        int FailedRecords,
        int SkippedRecords,
        string? ErrorMessage,
        ImportSummary? Summary
    ) : IDomainEvent;

  
}
