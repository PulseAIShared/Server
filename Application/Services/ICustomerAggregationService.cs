using DataTransferObjects.Customers;
using Domain.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ICustomerAggregationService
    {

        Task<CustomerResponse> GetUnifiedCustomerAsync(Guid customerId, Guid companyId);
        Task<Customer> AddOrUpdateCustomerDataAsync(Guid customerId, Dictionary<string, object> sourceData, string sourceName, string? importBatchId = null, Guid? importedByUserId = null);
        Task<Customer> SetPrimarySourceAsync(Guid customerId, string dataType, string sourceName);
        Task<bool> DeactivateSourceAsync(Guid customerId, string dataType, string sourceName);
        Task<List<CustomerCrmInfo>> GetAllCrmSourcesAsync(Guid customerId);
        Task<List<CustomerPaymentInfo>> GetAllPaymentSourcesAsync(Guid customerId);
        Task<decimal> CalculateAggregatedChurnRiskAsync(Guid customerId);
    }
}
