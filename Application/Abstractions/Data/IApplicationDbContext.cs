
using Domain.Customers;
using Domain.Imports;
using Domain.Integration;
using Domain.Notification;
using Domain.Segments;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Notification> Notifications { get; }

    DbSet<ImportJob> ImportJobs { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerActivity> CustomerActivities { get; }
    DbSet<ChurnPrediction> ChurnPredictions { get; }
    DbSet<CustomerSegment> CustomerSegments { get; }
    DbSet<CustomerCrmData> CustomerCrmData { get; set; }
     DbSet<CustomerPaymentData> CustomerPaymentData { get; set; }
     DbSet<CustomerMarketingData> CustomerMarketingData { get; set; }
     DbSet<CustomerSupportData> CustomerSupportData { get; set; }
     DbSet<CustomerEngagementData> CustomerEngagementData { get; set; }
    DbSet<Company> Companies { get; }
    DbSet<CompanyInvitation> CompanyInvitations { get; }
    DbSet<Integration> Integrations { get; }
    DatabaseFacade Database { get; }

    ChangeTracker ChangeTracker { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
