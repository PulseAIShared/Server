using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Login;

internal sealed class LoginUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider) : ICommandHandler<LoginUserCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
         .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginResponse>(UserErrors.NotFoundByEmail);
        }

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!verified)
        {
            return Result.Failure<LoginResponse>(UserErrors.NotFoundByEmail);
        }

        string accessToken = tokenProvider.Create(user);
        string refreshToken = tokenProvider.CreateRefreshToken();
        DateTime refreshTokenExpiryTime = tokenProvider.GetRefreshTokenExpiryTime();

        user.SetRefreshToken(refreshToken, refreshTokenExpiryTime);
        await context.SaveChangesAsync(cancellationToken);


        return Result.Success(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            RefreshTokenExpiryTime: refreshTokenExpiryTime
        ));
    }
}
