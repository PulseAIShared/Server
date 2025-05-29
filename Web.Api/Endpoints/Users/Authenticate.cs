using Application.Abstractions.Messaging;
using Application.Users.GetById;
using Domain.Users;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel;
using System.Reflection;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

    internal sealed class Authenticate : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("users/me", async (
                Guid userId,
                IQueryHandler<GetUserByIdQuery, UserResponse> handler,
                IMemoryCache cache,
                CancellationToken cancellationToken) =>
            {
                if (User.Identity.IsAuthenticated)
                {
                    if (!cache.TryGetValue($"user:{user.UserId}", out UserResponse? cachedUser))
                    {
                        var command = new GetUserByIdQuery(user.UserId);
                        Result<UserResponse> result = await handler.Handle(command, cancellationToken);

                        if (result.IsFailure)
                        {
                            return CustomResults.Problem(result);
                        }

                        cachedUser = result.Value;
                        cache.Set($"user:{user.UserId}", cachedUser, TimeSpan.FromMinutes(5));
                    }

                    return Results.Ok(new { user = cachedUser });
                }

                string? refreshToken = Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Results.Unauthorized();
                }

                var refreshCommand = new RefreshTokenCommand(refreshToken);
                Result<RefreshTokenWithUserResponse> refreshResult = await sender.Send(refreshCommand, cancellationToken);

                if (refreshResult.IsFailure)
                {
                    return CustomResults.Problem(refreshResult);
                }

                // Return both the new token and user data
                return Results.Ok(new
                {
                    token = refreshResult.Value.AccessToken,
                    user = refreshResult.Value.User
                });
            })
            .HasPermission(Permissions.UsersAccess)
            .WithTags(Tags.Users);
        }
    }

