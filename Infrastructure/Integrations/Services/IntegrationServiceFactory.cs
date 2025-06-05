using Infrastructure.Integrations.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Integrations.Services
{
    public class IntegrationServiceFactory : IIntegrationServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<IntegrationType, Type> _serviceTypes;

        public IntegrationServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _serviceTypes = new Dictionary<IntegrationType, Type>
        {
            { IntegrationType.HubSpot, typeof(HubSpotIntegrationService) },
            // Add other integration services here when implemented
            // { IntegrationType.Salesforce, typeof(SalesforceIntegrationService) },
            // { IntegrationType.Stripe, typeof(StripeIntegrationService) },
        };
        }

        public IIntegrationService GetService(IntegrationType type)
        {
            if (!_serviceTypes.TryGetValue(type, out var serviceType))
            {
                throw new NotSupportedException($"Integration type {type} is not supported");
            }

            return (IIntegrationService)_serviceProvider.GetRequiredService(serviceType);
        }

        public IEnumerable<IIntegrationService> GetAllServices()
        {
            return _serviceTypes.Values.Select(type => (IIntegrationService)_serviceProvider.GetRequiredService(type));
        }
    }

}
