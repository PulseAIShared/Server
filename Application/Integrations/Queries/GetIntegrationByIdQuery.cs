using Application.Abstractions.Messaging;
using DataTransferObjects.Integrations;
using DataTransferObjects.Users.Responses;

namespace Application.Integrations.GetById;

public sealed record GetIntegrationByIdQuery(Guid integrationId) : IQuery<IntegrationResponse>;
