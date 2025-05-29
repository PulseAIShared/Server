using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Users.Responses
{
    public class RefreshTokenWithUserResponse
    {
        public string AccessToken { get; set; }
        public UserResponse User { get; set; }
    }

}
