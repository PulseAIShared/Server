using SharedKernel.Enums;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public sealed record ImportJobStartedDomainEvent(
      Guid ImportJobId,
      Guid UserId,
      string FileName,
      ImportJobType Type,
      string? ImportSource
  ) : IDomainEvent;
}
