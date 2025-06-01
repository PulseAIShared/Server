using Application.Abstractions.Messaging;
using DataTransferObjects.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Queries
{
    public sealed record GetImportJobQuery(Guid ImportJobId) : IQuery<ImportJobResponse>;
}
