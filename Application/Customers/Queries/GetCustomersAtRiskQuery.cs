using Application.Abstractions.Messaging;
using DataTransferObjects.Common;
using DataTransferObjects.Customers;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Queries
{
    public sealed record GetCustomersAtRiskQuery(
       int Page,
       int PageSize,
       ChurnRiskLevel MinRiskLevel = ChurnRiskLevel.Medium
   ) : IQuery<PagedResult<CustomerResponse>>;
}
