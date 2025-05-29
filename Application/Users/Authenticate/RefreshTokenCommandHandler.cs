using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Users.Responses;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;


namespace Application.Users.Authenticate;

internal sealed class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    ITokenProvider tokenProvider) : ICommandHandler<RefreshTokenCommand, RefreshTokenWithUserResponse>
{
    public async Task<Result<RefreshTokenWithUserResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {

        User? user = await context.Users
            .SingleOrDefaultAsync(u => u.RefreshToken == command.RefreshToken, cancellationToken);

        if (user is null || !user.IsRefreshTokenValid())
        {
            return Result.Failure<RefreshTokenWithUserResponse>(UserErrors.RefreshTokenInvalid);
        }

        var userResponse = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateCreated = user.DateCreated,
            Role = user.Role.ToString()
        };
        string accessToken = tokenProvider.Create(user);

        return new RefreshTokenWithUserResponse
        {
            AccessToken = accessToken,
            User = userResponse
        };
    }
}