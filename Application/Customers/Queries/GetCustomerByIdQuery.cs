using Application.Abstractions.Messaging;
using DataTransferObjects.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Queries
{
    public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDetailResponse>;
}
