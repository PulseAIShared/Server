using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using DataTransferObjects.Integrations;
using DataTransferObjects.Users.Responses;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Integrations.GetById;

internal sealed class GetIntegrationByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetIntegrationByIdQuery, IntegrationResponse>
{
    public async Task<Result<IntegrationResponse>> Handle(GetIntegrationByIdQuery query, CancellationToken cancellationToken)
    {


        return new IntegrationResponse();
    }
}
