using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public sealed record ImportJobProgressDomainEvent(
       Guid ImportJobId,
       Guid UserId,
       int ProcessedRecords,
       int TotalRecords,
       double ProgressPercentage
   ) : IDomainEvent;
}
