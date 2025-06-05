using Application.Abstractions.Messaging;
using Application.Users.GetById;
using DataTransferObjects.Users.Responses;
using Web.Api.Endpoints.Users;
using Web.Api.Endpoints;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Extensions;
using Application.Integrations.Queries;
using DataTransferObjects.Integrations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("integrations", async (
            IQueryHandler<GetIntegrationsQuery, List<IntegrationResponse>> handler,
            CancellationToken cancellationToken,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null
            ) =>
        {
            // Fix for CS0119: Use typeof(IntegrationType) in Enum.Parse
            // Fix for CS1503: Convert 'status' to nullable IntegrationStatus using Enum.TryParse
            IntegrationType? integrationType = null;
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<IntegrationType>(type, out var parsedType))
            {
                integrationType = parsedType;
            }

            IntegrationStatus? integrationStatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<IntegrationStatus>(status, out var parsedStatus))
            {
                integrationStatus = parsedStatus;
            }

            var query = new GetIntegrationsQuery
            {
                Type = integrationType,
                Status = integrationStatus
            };

            Result<List<IntegrationResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .HasPermission(Permissions.UsersAccess)
        .WithTags(Tags.Users);
    }
}
