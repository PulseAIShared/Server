using Application.Abstractions.Messaging;
using DataTransferObjects.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Commands
{
    public sealed record DeleteCustomersCommand(List<Guid> CustomerIds) : ICommand<DeleteCustomersResponse>;
}
