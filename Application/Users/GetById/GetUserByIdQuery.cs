using Application.Abstractions.Messaging;
using DataTransferObjects.Users.Responses;

namespace Application.Users.GetById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
