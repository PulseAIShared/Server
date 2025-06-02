using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Users.Responses;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Enums;
using static Domain.Users.User;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // Check if email already exists
        if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return Result.Failure<RegisterUserResponse>(UserErrors.EmailNotUnique);
        }

        Company company;
        UserRole userRole;
        bool isCompanyOwner;

        // Scenario 1: User has invitation token (joining existing company)
        if (!string.IsNullOrEmpty(command.InvitationToken))
        {
            var invitation = await context.CompanyInvitations
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i =>
                    i.InvitationToken == command.InvitationToken &&
                    i.Email.ToLower() == command.Email.ToLower() &&
                    i.IsValid,
                    cancellationToken);

            if (invitation == null)
            {
                return Result.Failure<RegisterUserResponse>(
                    UserErrors.InvalidOrExpiredInvitation);
            }

            company = invitation.Company;
            userRole = invitation.InvitedRole;
            isCompanyOwner = false;

            // Check if company can accept more users
            if (!company.CanAddMoreUsers())
            {
                return Result.Failure<RegisterUserResponse>(
                    UserErrors.CompanyUserLimitReached);
            }

            // Create user for existing company
            var invitedUser = new User
            {
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                PasswordHash = passwordHasher.Hash(command.Password),
                Role = userRole,
                IsCompanyOwner = false,
                CompanyId = company.Id
            };

            context.Users.Add(invitedUser);

            // Mark invitation as accepted
            invitation.Accept(invitedUser.Id);

            invitedUser.Raise(new UserRegisteredDomainEvent(invitedUser.Id));

            await context.SaveChangesAsync(cancellationToken);

            return new RegisterUserResponse(
                invitedUser.Id,
                company.Id,
                company.Name,
                userRole,
                false
            );
        }
        // Scenario 2: Creating new company (user becomes owner)
        else if (!string.IsNullOrEmpty(command.CompanyName))
        {
            // Step 1: Create company without owner first
            company = new Company
            {
                Name = command.CompanyName,
                Size = command.CompanySize ?? CompanySize.Startup,
                Domain = command.CompanyDomain,
                Country = command.CompanyCountry ?? "US",
                Industry = command.CompanyIndustry,
                Plan = CompanyPlan.Free,
                OwnerId = null // Don't set owner yet - this breaks the circular dependency
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync(cancellationToken); // Save company first

            // Step 2: Create user with the company ID
            var user = new User
            {
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                PasswordHash = passwordHasher.Hash(command.Password),
                Role = UserRole.CompanyOwner,
                IsCompanyOwner = true,
                CompanyId = company.Id
            };

            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken); // Save user

            // Step 3: Update company with owner ID
            company.SetOwner(user.Id);
            await context.SaveChangesAsync(cancellationToken); // Final save

            user.Raise(new UserRegisteredDomainEvent(user.Id));

            return new RegisterUserResponse(
                user.Id,
                company.Id,
                company.Name,
                UserRole.CompanyOwner,
                true
            );
        }
        else
        {
            return Result.Failure<RegisterUserResponse>(
                UserErrors.CompanyInfoOrInvitationRequired);
        }
    }
}