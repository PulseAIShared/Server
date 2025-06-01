using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public sealed record ImportJobValidationCompletedDomainEvent(
      Guid ImportJobId,
      Guid UserId,
      int TotalRecords,
      int ValidationErrors,
      bool HasErrors
  ) : IDomainEvent;
}
