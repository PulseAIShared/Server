using Application.Abstractions.Messaging;
using DataTransferObjects.Integrations;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Integrations.Queries
{
    public sealed record GetIntegrationsQuery(
       IntegrationType? Type = null,
       IntegrationStatus? Status = null
   ) : IQuery<List<IntegrationResponse>>;
}
