using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Queries
{
    public sealed record GetUserImportHistoryQuery(int Page, int PageSize) : IQuery<PagedResult<ImportJobSummaryResponse>>;
}
