using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Users.Login;
using Application.Users.Logout;
using Domain.Users;
using SharedKernel;
using System.Reflection;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Logout : IEndpoint
{

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/logout", async (
            HttpContext httpContext,
            IUserContext user,

            ICommandHandler<LogoutCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            // Only revoke the token if the user is authenticated
            if (httpContext.User.Identity.IsAuthenticated)
            {
                var command = new LogoutCommand(user.UserId);
                Result<bool> result = await handler.Handle(command, cancellationToken);

                if (result.IsFailure)
                {
                    return CustomResults.Problem(result);
                }
            }

            return Results.Ok(new { message = "Logged out successfully" });
        })
        .WithTags(Tags.Users);
    }
}
