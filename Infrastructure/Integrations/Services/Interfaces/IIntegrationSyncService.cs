using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Services.Interfaces
{
    public interface IIntegrationSyncService
    {
        Task SyncAllIntegrationsAsync();
        Task SyncIntegrationAsync(Guid integrationId);
        Task ScheduleAutomaticSyncAsync(Guid integrationId);
        Task DisableAutomaticSyncAsync(Guid integrationId);
    }
}
