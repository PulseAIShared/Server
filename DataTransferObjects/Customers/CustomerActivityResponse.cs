using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class CustomerActivityResponse
    {
        public Guid Id { get; set; }
        public ActivityType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}
