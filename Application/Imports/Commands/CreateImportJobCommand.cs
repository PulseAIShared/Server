using Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Http;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Commands
{
    public sealed record CreateImportJobCommand(
     Guid UserId,
     IFormFile File,
     ImportJobType Type,
     string ImportSource,
     bool SkipDuplicates
 ) : ICommand<Guid>;
}
