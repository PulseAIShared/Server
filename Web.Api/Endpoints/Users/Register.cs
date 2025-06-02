using Application.Abstractions.Messaging;
using Application.Users.Register;
using DataTransferObjects.Users.Responses;
using SharedKernel;
using SharedKernel.Enums;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Register : IEndpoint
{
    public sealed record Request(
    string Email,
     string FirstName,
     string LastName,
     string Password,
     string? CompanyName = null,
     string? CompanyDomain = null,
     string? CompanyCountry = null,
     CompanySize? CompanySize = null,
     string? CompanyIndustry = null,
     string? InvitationToken = null
 );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/register", async (
            Request request,
            ICommandHandler<RegisterUserCommand, RegisterUserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password,
                request.CompanyName,
                request.CompanyDomain,
                request.CompanyCountry,
                request.CompanySize,
                request.CompanyIndustry,
                request.InvitationToken
               );

            Result<RegisterUserResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users);
    }
}
