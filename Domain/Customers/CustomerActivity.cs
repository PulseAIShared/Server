using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Customers
{
    public class CustomerActivity : Entity
    {
        [Required]
        public Guid CustomerId { get; set; } = Guid.Empty;

        [Required]
        public ActivityType Type { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Customer Customer { get; set; } = null!;
    }

}
