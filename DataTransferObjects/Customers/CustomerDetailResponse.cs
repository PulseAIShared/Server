using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerDetailResponse : CustomerResponse
    {

        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? TimeZone { get; set; }

        // Recent activities (last 10)
        public List<CustomerActivityResponse> RecentActivities { get; set; } = new();

        // Churn prediction history (last 5)
        public List<ChurnPredictionResponse> ChurnHistory { get; set; } = new();
    }
}
