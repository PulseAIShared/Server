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

        // Make OwnerId nullable to break circular dependency
        public Guid? OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public CompanyPlan Plan { get; set; } = CompanyPlan.Free;

        // Navigation properties
        public User? Owner { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<CompanyInvitation> Invitations { get; set; } = new List<CompanyInvitation>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<CustomerSegment> Segments { get; set; } = new List<CustomerSegment>();
        public ICollection<Domain.Integration.Integration> Integrations { get; set; } = new List<Domain.Integration.Integration>();
        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
        public ICollection<ImportJob> ImportJobs { get; set; } = new List<ImportJob>();

        // Business logic for user limits based on plan
        public int MaxUsers => Plan switch
        {
            CompanyPlan.Free => 3,
            CompanyPlan.Starter => 10,
            CompanyPlan.Professional => 50,
            CompanyPlan.Enterprise => int.MaxValue,
            _ => 3
        };

        public bool CanAddMoreUsers() => Users.Count < MaxUsers;

        // Method to set owner after company creation
        public void SetOwner(Guid userId)
        {
            OwnerId = userId;
        }
    }
}