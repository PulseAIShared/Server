using Application.Abstractions.Messaging;
using Application.Users.Login;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Login : IEndpoint
{
    public sealed record Request(string Email, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login", async (
            HttpContext httpContext, 
            Request request,
            ICommandHandler<LoginUserCommand, LoginResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginUserCommand(request.Email, request.Password);

            Result<LoginResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
              response =>
              {
                  var cookieOptions = new CookieOptions
                  {
                      HttpOnly = true,
                      Secure = true,
                      SameSite = SameSiteMode.None,
                      Expires = response.RefreshTokenExpiryTime
                  };

                  httpContext.Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions); 

                  return Results.Ok(new
                  {
                      jwt = response.AccessToken,
                      message = "Login successful"
                  });
              },
              CustomResults.Problem
          );
        })
        .WithTags(Tags.Users);
    }
}
