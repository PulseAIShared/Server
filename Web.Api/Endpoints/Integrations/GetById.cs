using Application.Abstractions.Messaging;
using Application.Integrations.GetById;
using DataTransferObjects.Integrations;
using SharedKernel;
using Web.Api.Endpoints.Users;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Integrations
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("integrations/{integrationId}", async (
                Guid integrationId,
                IQueryHandler<GetIntegrationByIdQuery, IntegrationResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetIntegrationByIdQuery(integrationId);
                Result<IntegrationResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .HasPermission(Permissions.IntegrationsAccess)
            .WithTags(Tags.Integrations);
        }
    }
}
