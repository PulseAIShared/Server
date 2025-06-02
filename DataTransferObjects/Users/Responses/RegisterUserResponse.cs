using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain.Users.User;

namespace DataTransferObjects.Users.Responses
{
   public sealed record RegisterUserResponse(
        Guid UserId,
        Guid CompanyId,
        string CompanyName,
        UserRole Role,
        bool IsCompanyOwner
    );
}
