using Application.Abstractions.Messaging;
using DataTransferObjects.Users.Responses;

namespace Application.Users.GetByEmail;

public sealed record GetUserByEmailQuery(string Email) : IQuery<UserResponse>;
