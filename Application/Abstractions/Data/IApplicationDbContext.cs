
using Domain.Customers;
using Domain.Imports;
using Domain.Integration;
using Domain.Notification;
using Domain.Segments;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

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
    DbSet<Company> Companies { get; }
    DbSet<CompanyInvitation> CompanyInvitations { get; }
    DbSet<Integration> Integrations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
