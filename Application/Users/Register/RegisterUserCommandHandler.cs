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

            // Mark invitation as accepted (will be set after user creation)
        }
        // Scenario 2: Creating new company (user becomes owner)
        else if (!string.IsNullOrEmpty(command.CompanyName))
        {
            company = new Company
            {
                Name = command.CompanyName,
                Size = command.CompanySize ?? CompanySize.Startup,
                Domain = command.CompanyDomain,
                Country = command.CompanyCountry ?? "US",
                Industry = command.CompanyIndustry,
                Plan = CompanyPlan.Free // Always start with free plan
            };

            context.Companies.Add(company);
            userRole = User.UserRole.CompanyOwner;
            isCompanyOwner = true;
        }
        else
        {
            return Result.Failure<RegisterUserResponse>(
                UserErrors.CompanyInfoOrInvitationRequired);
        }

        // Create the user
        var user = new User
        {
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PasswordHash = passwordHasher.Hash(command.Password),
            Role = userRole,
            IsCompanyOwner = isCompanyOwner,
        };

        // Set company relationships
        if (isCompanyOwner)
        {
            company.OwnerId = user.Id;
            user.CompanyId = company.Id;
        }
        else
        {
            user.CompanyId = company.Id;

            // Accept the invitation
            var invitation = await context.CompanyInvitations
                .FirstAsync(i => i.InvitationToken == command.InvitationToken!,
                    cancellationToken);
            invitation.Accept(user.Id);
        }

        context.Users.Add(user);
        user.Raise(new UserRegisteredDomainEvent(user.Id));

        await context.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse(
            user.Id,
            company.Id,
            company.Name,
            userRole,
            isCompanyOwner
        );
    }
}
