using Domain.Customers;
using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Users
{
    public class Company : Entity
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Domain { get; set; }

        public CompanySize Size { get; set; }

        public string? Industry { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<CustomerSegment> Segments { get; set; } = new List<CustomerSegment>();
    }
}
