using Application.Abstractions.Messaging;
using Application.Users.Authenticate;
using Application.Users.GetById;
using DataTransferObjects.Users.Responses;
using Domain.Users;
using Microsoft.AspNetCore.Http;
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
            HttpContext httpContext,
            Guid userId,
            ICommandHandler<RefreshTokenCommand, RefreshTokenWithUserResponse> commandHandler,
            IQueryHandler<GetUserByIdQuery, UserResponse> queryHandler,
            IMemoryCache cache,
            CancellationToken cancellationToken) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                Guid parsedUserId;
                if (!Guid.TryParse(httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out parsedUserId))
                {
                    return Results.Unauthorized();
                }

                if (!cache.TryGetValue($"user:{parsedUserId}", out UserResponse? cachedUser))
                {
                    var query = new GetUserByIdQuery(parsedUserId);
                    Result<UserResponse> result = await queryHandler.Handle(query, cancellationToken);

                    if (result.IsFailure)
                    {
                        return CustomResults.Problem(result);
                    }

                    cachedUser = result.Value;
                    cache.Set($"user:{parsedUserId}", cachedUser, TimeSpan.FromMinutes(5));
                }

                return Results.Ok(new { user = cachedUser });
            }

            string? refreshToken = httpContext.Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Results.Unauthorized();
            }

            var refreshCommand = new RefreshTokenCommand(refreshToken);
            Result<RefreshTokenWithUserResponse> refreshResult = await commandHandler.Handle(refreshCommand, cancellationToken);

            if (refreshResult.IsFailure)
            {
                return CustomResults.Problem(refreshResult);
            }

            return Results.Ok(new
            {
                token = refreshResult.Value.AccessToken,
                user = refreshResult.Value.User
            });
        })
        .WithTags(Tags.Users);
    }
}

