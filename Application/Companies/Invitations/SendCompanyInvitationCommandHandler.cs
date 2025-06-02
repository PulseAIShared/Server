using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Companies.Invitations
{
    internal sealed class SendCompanyInvitationCommandHandler(
      IApplicationDbContext context,
      IUserContext userContext)
      : ICommandHandler<SendCompanyInvitationCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            SendCompanyInvitationCommand command,
            CancellationToken cancellationToken)
        {
            // Get current user and their company
            var user = await context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

            if (user?.Company == null)
            {
                return Result.Failure<Guid>(UserErrors.UserNotInCompany);
            }

            // Check permissions (only admins/owners can invite)
            if (user.Role < User.UserRole.Admin)
            {
                return Result.Failure<Guid>(UserErrors.InsufficientPermissions);
            }

            // Check if company can accept more users
            if (!user.Company.CanAddMoreUsers())
            {
                return Result.Failure<Guid>(UserErrors.CompanyUserLimitReached);
            }

            // Check if user already exists or has pending invitation
            var existingUser = await context.Users
                .AnyAsync(u => u.Email == command.Email.ToLower(), cancellationToken);

            if (existingUser)
            {
                return Result.Failure<Guid>(UserErrors.UserAlreadyExists);
            }

            var existingInvitation = await context.CompanyInvitations
                .AnyAsync(i =>
                    i.Email == command.Email.ToLower() &&
                    i.CompanyId == user.CompanyId &&
                    i.IsValid,
                    cancellationToken);

            if (existingInvitation)
            {
                return Result.Failure<Guid>(UserErrors.InvitationAlreadySent);
            }

            // Create invitation
            var invitation = CompanyInvitation.Create(
                user.CompanyId,
                command.Email,
                command.Role,
                user.Id
            );

            context.CompanyInvitations.Add(invitation);
            await context.SaveChangesAsync(cancellationToken);

            // Send invitation email
            //await emailService.SendCompanyInvitationAsync(
            //    command.Email,
            //    user.Company.Name,
            //    user.FirstName + " " + user.LastName,
            //    invitation.InvitationToken
            //);

            return invitation.Id;
        }
    }
}
