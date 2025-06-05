using DataTransferObjects.Integrations;
using Domain.Integration;
using Infrastructure.Integrations.Models;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Services.Interfaces
{
    public interface IIntegrationService
    {
        IntegrationType Type { get; }
        Task<bool> TestConnectionAsync(Integration integration);
        Task<SyncResult> SyncCustomersAsync(Integration integration, SyncOptions options);
    }

}
