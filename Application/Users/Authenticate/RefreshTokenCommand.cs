using Application.Abstractions.Messaging;
using DataTransferObjects.Users.Responses;


namespace Application.Users.Authenticate;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenWithUserResponse>;

