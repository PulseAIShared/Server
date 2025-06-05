using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Services.Interfaces
{
    public interface IIntegrationServiceFactory
    {
        IIntegrationService GetService(IntegrationType type);
        IEnumerable<IIntegrationService> GetAllServices();
    }
}
