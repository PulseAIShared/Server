using Application.Abstractions.Messaging;
using Domain.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain.Users.User;

namespace Application.Companies.Invitations
{
    public sealed record SendCompanyInvitationCommand(
       string Email,
       UserRole Role = User.UserRole.User
   ) : ICommand<Guid>;

}
