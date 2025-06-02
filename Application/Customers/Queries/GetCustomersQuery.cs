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
    public sealed record GetCustomersQuery(
        int Page,
        int PageSize,
        string? Search = null,
        SubscriptionStatus? SubscriptionStatus = null,
        SubscriptionPlan? Plan = null,
        PaymentStatus? PaymentStatus = null,
        ChurnRiskLevel? ChurnRiskLevel = null,
        string? SortBy = null,
        bool SortDescending = false
    ) : IQuery<PagedResult<CustomerResponse>>;
}
