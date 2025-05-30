using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Logout;

internal sealed class LogoutUserCommandHandler(
    IApplicationDbContext context) : ICommandHandler<LogoutCommand, bool>
{
    public async Task<Result<bool>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<bool>(UserErrors.NotFound(command.UserId));
        }

        // Revoke the refresh token
        user.RevokeRefreshToken();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}