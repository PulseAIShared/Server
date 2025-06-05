using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed record DeleteCustomersResponse(
        int TotalRequested,
        int SuccessfullyDeleted,
        int Failed,
        List<CustomerDeletionError> Errors
    );

    public sealed record CustomerDeletionError(
        Guid CustomerId,
        string Email,
        string ErrorMessage
    );
}
