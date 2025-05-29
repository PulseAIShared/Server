using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Users.Responses
{
   public class UserResponse
    {
        public Guid Id { get; init; }

        public string Email { get; init; }

        public string FirstName { get; init; }

        public string LastName { get; init; }
        public DateTime DateCreated { get; init; }

        public string Role { get; init; }
    }
}
