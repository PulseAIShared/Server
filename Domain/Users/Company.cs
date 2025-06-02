using Domain.Campaigns;
using Domain.Customers;
using Domain.Imports;
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

        public string? Country { get; set; }

        public Guid OwnerId { get; set; }  // The user who owns this company
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public CompanyPlan Plan { get; set; } = CompanyPlan.Free;
        public int MaxUsers { get; set; } = 5; // Based on plan

        // Navigation properties
        public User Owner { get; set; } = null!;
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<CompanyInvitation> Invitations { get; set; } = new List<CompanyInvitation>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<CustomerSegment> Segments { get; set; } = new List<CustomerSegment>();
        public ICollection<Domain.Integration.Integration> Integrations { get; set; } = new List<Domain.Integration.Integration>();
        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public ICollection<ImportJob> ImportJobs { get; set; } = new List<ImportJob>();

        public bool CanAddMoreUsers() => Users.Count < MaxUsers;
    }
}
