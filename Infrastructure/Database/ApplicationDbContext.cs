using Application.Abstractions.Data;
using Domain.Analytics;
using Domain.Campaigns;
using Domain.Customers;
using Domain.Imports;
using Domain.Integration;
using Domain.Notification;
using Domain.Segments;
using Domain.Todos;
using Domain.Users;
using Infrastructure.Database.Configurations;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using SharedKernel;


namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<CompanyInvitation> CompanyInvitations { get; set; }


    // Customer Management
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerActivity> CustomerActivities { get; set; } = null!;
    public DbSet<ChurnPrediction> ChurnPredictions { get; set; } = null!;

    // Segmentation
    public DbSet<CustomerSegment> CustomerSegments { get; set; } = null!;
    public DbSet<SegmentCriteria> SegmentCriteria { get; set; } = null!;

    // Campaign Management
    public DbSet<Campaign> Campaigns { get; set; } = null!;
    public DbSet<CampaignStep> CampaignSteps { get; set; } = null!;

    // Analytics & Reporting
    public DbSet<DashboardMetrics> DashboardMetrics { get; set; } = null!;

    // Integration Management
    public DbSet<Integration> Integrations { get; set; } = null!;

    // Import Management
    public DbSet<ImportJob> ImportJobs { get; set; } = null!;

    // Notifications
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerActivityConfiguration());
        modelBuilder.ApplyConfiguration(new ChurnPredictionConfiguration());
        modelBuilder.ApplyConfiguration(new SegmentConfiguration());
        modelBuilder.ApplyConfiguration(new SegmentCriteriaConfiguration());
        modelBuilder.ApplyConfiguration(new CampaignConfiguration());
        modelBuilder.ApplyConfiguration(new CampaignStepConfiguration());
        modelBuilder.ApplyConfiguration(new IntegrationConfiguration());
        modelBuilder.ApplyConfiguration(new DashboardMetricsConfiguration());
        modelBuilder.ApplyConfiguration(new ImportJobConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyInvitationConfiguration());

        modelBuilder.HasDefaultSchema(Schemas.Default);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}
