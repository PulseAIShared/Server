using Domain.Customers;
using Domain.Segments;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;
namespace Infrastructure.Database.Configurations;

    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(c => c.CompanyId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(c => c.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.CompanyName)
            .HasMaxLength(200);

        builder.Property(c => c.JobTitle)
            .HasMaxLength(100);

        // Subscription properties
        builder.Property(c => c.SubscriptionStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Plan)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.MonthlyRecurringRevenue)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(c => c.LifetimeValue)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(c => c.SubscriptionStartDate)
            .HasColumnType("date");

        builder.Property(c => c.SubscriptionEndDate)
            .HasColumnType("date");

        // Engagement properties
        builder.Property(c => c.LastLoginDate)
            .HasColumnType("timestamp");

        builder.Property(c => c.WeeklyLoginFrequency)
            .HasDefaultValue(0);

        builder.Property(c => c.FeatureUsagePercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(c => c.SupportTicketCount)
            .HasDefaultValue(0);

        // Churn prediction properties
        builder.Property(c => c.ChurnRiskScore)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(c => c.ChurnRiskLevel)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(ChurnRiskLevel.Low);

        builder.Property(c => c.ChurnPredictionDate)
            .HasColumnType("timestamp");

        // Demographics
        builder.Property(c => c.Age);

        builder.Property(c => c.Gender)
            .HasMaxLength(20);

        builder.Property(c => c.Location)
            .HasMaxLength(100);

        builder.Property(c => c.Country)
            .HasMaxLength(2); // ISO 2-letter code

        builder.Property(c => c.TimeZone)
            .HasMaxLength(50);

        // Payment properties
        builder.Property(c => c.PaymentStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.LastPaymentDate)
            .HasColumnType("date");

        builder.Property(c => c.NextBillingDate)
            .HasColumnType("date");

        builder.Property(c => c.PaymentFailureCount)
            .HasDefaultValue(0);

        builder.Property(c => c.LastPaymentFailureDate)
            .HasColumnType("date");

        // Sync metadata
        builder.Property(c => c.LastSyncedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.SyncVersion)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(c => new { c.CompanyId, c.ExternalId, c.Source })
            .IsUnique()
            .HasDatabaseName("ix_customers_company_external_source");

        builder.HasIndex(c => c.Email)
            .HasDatabaseName("ix_customers_email");

        builder.HasIndex(c => c.ChurnRiskScore)
            .HasDatabaseName("ix_customers_churn_risk_score");

        builder.HasIndex(c => c.SubscriptionStatus)
            .HasDatabaseName("ix_customers_subscription_status");

        builder.HasIndex(c => c.PaymentStatus)
            .HasDatabaseName("ix_customers_payment_status");

        builder.HasIndex(c => c.LastLoginDate)
            .HasDatabaseName("ix_customers_last_login");

        builder.HasIndex(c => c.CompanyId)
            .HasDatabaseName("ix_customers_company_id");

        // Relationships
        builder.HasOne(c => c.Company)
            .WithMany(co => co.Customers)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Activities)
            .WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ChurnPredictions)
            .WithOne(cp => cp.Customer)
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("customers");
    }
}

public class CustomerActivityConfiguration : IEntityTypeConfiguration<CustomerActivity>
{
    public void Configure(EntityTypeBuilder<CustomerActivity> builder)
    {
        builder.HasKey(ca => ca.Id);

        builder.Property(ca => ca.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(ca => ca.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(ca => ca.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ca => ca.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ca => ca.Metadata)
            .HasColumnType("jsonb");

        builder.Property(ca => ca.ActivityDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(ca => ca.CustomerId)
            .HasDatabaseName("ix_customer_activities_customer_id");

        builder.HasIndex(ca => ca.Type)
            .HasDatabaseName("ix_customer_activities_type");

        builder.HasIndex(ca => ca.ActivityDate)
            .HasDatabaseName("ix_customer_activities_activity_date");

        builder.HasIndex(ca => new { ca.CustomerId, ca.ActivityDate })
            .HasDatabaseName("ix_customer_activities_customer_date");

        // Relationships
        builder.HasOne(ca => ca.Customer)
            .WithMany(c => c.Activities)
            .HasForeignKey(ca => ca.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("customer_activities");
    }
}

public class ChurnPredictionConfiguration : IEntityTypeConfiguration<ChurnPrediction>
{
    public void Configure(EntityTypeBuilder<ChurnPrediction> builder)
    {
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(cp => cp.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(cp => cp.RiskScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(cp => cp.RiskLevel)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cp => cp.PredictionDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(cp => cp.RiskFactors)
            .HasColumnType("jsonb");

        builder.Property(cp => cp.ModelVersion)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(cp => cp.CustomerId)
            .HasDatabaseName("ix_churn_predictions_customer_id");

        builder.HasIndex(cp => cp.PredictionDate)
            .HasDatabaseName("ix_churn_predictions_prediction_date");

        builder.HasIndex(cp => new { cp.CustomerId, cp.PredictionDate })
            .HasDatabaseName("ix_churn_predictions_customer_date");

        // Relationships
        builder.HasOne(cp => cp.Customer)
            .WithMany(c => c.ChurnPredictions)
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("churn_predictions");
    }
}