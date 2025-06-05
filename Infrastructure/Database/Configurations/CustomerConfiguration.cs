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

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Primary source tracking
        builder.Property(c => c.PrimaryCrmSource)
            .HasMaxLength(50);

        builder.Property(c => c.PrimaryPaymentSource)
            .HasMaxLength(50);

        builder.Property(c => c.PrimaryMarketingSource)
            .HasMaxLength(50);

        builder.Property(c => c.PrimarySupportSource)
            .HasMaxLength(50);

        builder.Property(c => c.PrimaryEngagementSource)
            .HasMaxLength(50);

        // Churn risk fields
        builder.Property(c => c.ChurnRiskScore)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(c => c.ChurnRiskLevel)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(c => c.Email)
            .HasDatabaseName("ix_customers_email");

        builder.HasIndex(c => new { c.CompanyId, c.Email })
            .IsUnique()
            .HasDatabaseName("ix_customers_company_email");

        builder.HasIndex(c => c.ChurnRiskScore)
            .HasDatabaseName("ix_customers_churn_risk_score");

        builder.HasIndex(c => c.PrimaryCrmSource)
            .HasDatabaseName("ix_customers_primary_crm_source");

        builder.HasIndex(c => c.PrimaryPaymentSource)
            .HasDatabaseName("ix_customers_primary_payment_source");

        // Relationships - Now ONE-TO-MANY
        builder.HasOne(c => c.Company)
            .WithMany(co => co.Customers)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.CrmDataSources)
            .WithOne(cd => cd.Customer)
            .HasForeignKey(cd => cd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.PaymentDataSources)
            .WithOne(pd => pd.Customer)
            .HasForeignKey(pd => pd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.MarketingDataSources)
            .WithOne(md => md.Customer)
            .HasForeignKey(md => md.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.SupportDataSources)
            .WithOne(sd => sd.Customer)
            .HasForeignKey(sd => sd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.EngagementDataSources)
            .WithOne(ed => ed.Customer)
            .HasForeignKey(ed => ed.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

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

public class CustomerCrmDataConfiguration : IEntityTypeConfiguration<CustomerCrmData>
{
    public void Configure(EntityTypeBuilder<CustomerCrmData> builder)
    {
        builder.HasKey(cd => cd.Id);

        builder.Property(cd => cd.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(cd => cd.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(cd => cd.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cd => cd.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cd => cd.IsPrimarySource)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(cd => cd.SourcePriority)
            .HasDefaultValue(0);

        builder.Property(cd => cd.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(cd => cd.ImportBatchId)
            .HasMaxLength(50);

        builder.Property(cd => cd.ImportedByUserId)
            .HasMaxLength(36);

        builder.Property(cd => cd.LeadSource)
            .HasMaxLength(100);

        builder.Property(cd => cd.LifecycleStage)
            .HasMaxLength(50);

        builder.Property(cd => cd.LeadStatus)
            .HasMaxLength(50);

        builder.Property(cd => cd.SalesOwnerId)
            .HasMaxLength(50);

        builder.Property(cd => cd.SalesOwnerName)
            .HasMaxLength(100);

        builder.Property(cd => cd.TotalDealValue)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(cd => cd.WonDealValue)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(cd => cd.CustomFields)
            .HasColumnType("jsonb");

        builder.Property(cd => cd.LastSyncedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(cd => cd.SyncVersion)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(cd => cd.CustomerId)
            .HasDatabaseName("ix_customer_crm_data_customer_id");

        builder.HasIndex(cd => new { cd.CustomerId, cd.Source })
            .HasDatabaseName("ix_customer_crm_data_customer_source");

        builder.HasIndex(cd => new { cd.Source, cd.ExternalId })
            .HasDatabaseName("ix_customer_crm_data_source_external_id");

        builder.HasIndex(cd => cd.ImportBatchId)
            .HasDatabaseName("ix_customer_crm_data_import_batch");

        builder.HasIndex(cd => new { cd.CustomerId, cd.IsPrimarySource })
            .HasDatabaseName("ix_customer_crm_data_customer_primary");

        builder.HasIndex(cd => cd.IsActive)
            .HasDatabaseName("ix_customer_crm_data_is_active");

        // Relationships
        builder.HasOne(cd => cd.Customer)
            .WithMany(c => c.CrmDataSources)
            .HasForeignKey(cd => cd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cd => cd.ImportedBy)
            .WithMany()
            .HasForeignKey(cd => cd.ImportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("customer_crm_data");
    }
}

public class CustomerPaymentDataConfiguration : IEntityTypeConfiguration<CustomerPaymentData>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentData> builder)
    {
        builder.HasKey(pd => pd.Id);

        builder.Property(pd => pd.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(pd => pd.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(pd => pd.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pd => pd.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pd => pd.IsPrimarySource)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pd => pd.SourcePriority)
            .HasDefaultValue(0);

        builder.Property(pd => pd.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pd => pd.ImportBatchId)
            .HasMaxLength(50);

        builder.Property(pd => pd.ImportedByUserId)
            .HasMaxLength(36);

        builder.Property(pd => pd.SubscriptionStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pd => pd.Plan)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pd => pd.MonthlyRecurringRevenue)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(pd => pd.LifetimeValue)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(pd => pd.PaymentStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pd => pd.CurrentBalance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(pd => pd.Currency)
            .HasMaxLength(3);

        builder.Property(pd => pd.BillingInterval)
            .HasMaxLength(20);

        builder.Property(pd => pd.PaymentMethodType)
            .HasMaxLength(50);

        builder.Property(pd => pd.LastSyncedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(pd => pd.CustomerId)
            .HasDatabaseName("ix_customer_payment_data_customer_id");

        builder.HasIndex(pd => new { pd.CustomerId, pd.Source })
            .HasDatabaseName("ix_customer_payment_data_customer_source");

        builder.HasIndex(pd => new { pd.Source, pd.ExternalId })
            .HasDatabaseName("ix_customer_payment_data_source_external_id");

        builder.HasIndex(pd => pd.ImportBatchId)
            .HasDatabaseName("ix_customer_payment_data_import_batch");

        builder.HasIndex(pd => new { pd.CustomerId, pd.IsPrimarySource })
            .HasDatabaseName("ix_customer_payment_data_customer_primary");

        builder.HasIndex(pd => pd.SubscriptionStatus)
            .HasDatabaseName("ix_customer_payment_data_subscription_status");

        builder.HasIndex(pd => pd.PaymentStatus)
            .HasDatabaseName("ix_customer_payment_data_payment_status");

        // Relationships
        builder.HasOne(pd => pd.Customer)
            .WithMany(c => c.PaymentDataSources)
            .HasForeignKey(pd => pd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pd => pd.ImportedBy)
            .WithMany()
            .HasForeignKey(pd => pd.ImportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("customer_payment_data");
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

public class CustomerMarketingDataConfiguration : IEntityTypeConfiguration<CustomerMarketingData>
{
    public void Configure(EntityTypeBuilder<CustomerMarketingData> builder)
    {
        builder.HasKey(md => md.Id);

        builder.Property(md => md.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(md => md.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(md => md.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(md => md.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(md => md.IsPrimarySource)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(md => md.SourcePriority)
            .HasDefaultValue(0);

        builder.Property(md => md.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(md => md.ImportBatchId)
            .HasMaxLength(50);

        builder.Property(md => md.ImportedByUserId)
            .HasMaxLength(36);

        // Email marketing metrics
        builder.Property(md => md.IsSubscribed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(md => md.SubscriptionDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(md => md.UnsubscriptionDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(md => md.AverageOpenRate)
            .HasColumnType("decimal(5,4)")  // Allows for 99.99% precision
            .HasDefaultValue(0);

        builder.Property(md => md.AverageClickRate)
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(0);

        builder.Property(md => md.TotalEmailsSent)
            .HasDefaultValue(0);

        builder.Property(md => md.TotalEmailsOpened)
            .HasDefaultValue(0);

        builder.Property(md => md.TotalEmailsClicked)
            .HasDefaultValue(0);

        builder.Property(md => md.LastEmailOpenDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(md => md.LastEmailClickDate)
            .HasColumnType("timestamp with time zone");

        // Campaign engagement
        builder.Property(md => md.CampaignCount)
            .HasDefaultValue(0);

        builder.Property(md => md.LastCampaignEngagement)
            .HasColumnType("timestamp with time zone");

        // Segmentation - JSON arrays
        builder.Property(md => md.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

        builder.Property(md => md.Lists)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

        // Sync metadata
        builder.Property(md => md.LastSyncedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(md => md.SyncVersion)
            .HasMaxLength(50);

        // Indexes for performance
        builder.HasIndex(md => md.CustomerId)
            .HasDatabaseName("ix_customer_marketing_data_customer_id");

        builder.HasIndex(md => new { md.CustomerId, md.Source })
            .HasDatabaseName("ix_customer_marketing_data_customer_source");

        builder.HasIndex(md => new { md.Source, md.ExternalId })
            .HasDatabaseName("ix_customer_marketing_data_source_external_id");

        builder.HasIndex(md => md.ImportBatchId)
            .HasDatabaseName("ix_customer_marketing_data_import_batch");

        builder.HasIndex(md => new { md.CustomerId, md.IsPrimarySource })
            .HasDatabaseName("ix_customer_marketing_data_customer_primary");

        builder.HasIndex(md => md.IsSubscribed)
            .HasDatabaseName("ix_customer_marketing_data_is_subscribed");

        builder.HasIndex(md => md.IsActive)
            .HasDatabaseName("ix_customer_marketing_data_is_active");

        builder.HasIndex(md => md.LastEmailOpenDate)
            .HasDatabaseName("ix_customer_marketing_data_last_email_open");

        builder.HasIndex(md => md.AverageOpenRate)
            .HasDatabaseName("ix_customer_marketing_data_open_rate");

        // Relationships
        builder.HasOne(md => md.Customer)
            .WithMany(c => c.MarketingDataSources)
            .HasForeignKey(md => md.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(md => md.ImportedBy)
            .WithMany()
            .HasForeignKey(md => md.ImportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("customer_marketing_data");
    }
}

public class CustomerSupportDataConfiguration : IEntityTypeConfiguration<CustomerSupportData>
{
    public void Configure(EntityTypeBuilder<CustomerSupportData> builder)
    {
        builder.HasKey(sd => sd.Id);

        builder.Property(sd => sd.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(sd => sd.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(sd => sd.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sd => sd.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sd => sd.IsPrimarySource)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sd => sd.SourcePriority)
            .HasDefaultValue(0);

        builder.Property(sd => sd.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sd => sd.ImportBatchId)
            .HasMaxLength(50);

        builder.Property(sd => sd.ImportedByUserId)
            .HasMaxLength(36);

        // Support metrics
        builder.Property(sd => sd.TotalTickets)
            .HasDefaultValue(0);

        builder.Property(sd => sd.OpenTickets)
            .HasDefaultValue(0);

        builder.Property(sd => sd.ClosedTickets)
            .HasDefaultValue(0);

        builder.Property(sd => sd.FirstTicketDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(sd => sd.LastTicketDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(sd => sd.AverageResolutionTime)
            .HasColumnType("decimal(8,2)")  // Hours with 2 decimal places
            .HasDefaultValue(0);

        builder.Property(sd => sd.CustomerSatisfactionScore)
            .HasColumnType("decimal(3,2)")  // 0.00 to 5.00 scale
            .HasDefaultValue(0);

        // Ticket priority breakdown
        builder.Property(sd => sd.LowPriorityTickets)
            .HasDefaultValue(0);

        builder.Property(sd => sd.MediumPriorityTickets)
            .HasDefaultValue(0);

        builder.Property(sd => sd.HighPriorityTickets)
            .HasDefaultValue(0);

        builder.Property(sd => sd.UrgentTickets)
            .HasDefaultValue(0);

        // Support categories - JSON dictionary
        builder.Property(sd => sd.TicketsByCategory)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions)null) : null);

        // Sync metadata
        builder.Property(sd => sd.LastSyncedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(sd => sd.SyncVersion)
            .HasMaxLength(50);

        // Indexes for performance
        builder.HasIndex(sd => sd.CustomerId)
            .HasDatabaseName("ix_customer_support_data_customer_id");

        builder.HasIndex(sd => new { sd.CustomerId, sd.Source })
            .HasDatabaseName("ix_customer_support_data_customer_source");

        builder.HasIndex(sd => new { sd.Source, sd.ExternalId })
            .HasDatabaseName("ix_customer_support_data_source_external_id");

        builder.HasIndex(sd => sd.ImportBatchId)
            .HasDatabaseName("ix_customer_support_data_import_batch");

        builder.HasIndex(sd => new { sd.CustomerId, sd.IsPrimarySource })
            .HasDatabaseName("ix_customer_support_data_customer_primary");

        builder.HasIndex(sd => sd.OpenTickets)
            .HasDatabaseName("ix_customer_support_data_open_tickets");

        builder.HasIndex(sd => sd.CustomerSatisfactionScore)
            .HasDatabaseName("ix_customer_support_data_csat_score");

        builder.HasIndex(sd => sd.LastTicketDate)
            .HasDatabaseName("ix_customer_support_data_last_ticket");

        builder.HasIndex(sd => sd.IsActive)
            .HasDatabaseName("ix_customer_support_data_is_active");

        // Relationships
        builder.HasOne(sd => sd.Customer)
            .WithMany(c => c.SupportDataSources)
            .HasForeignKey(sd => sd.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sd => sd.ImportedBy)
            .WithMany()
            .HasForeignKey(sd => sd.ImportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("customer_support_data");
    }
}


public class CustomerEngagementDataConfiguration : IEntityTypeConfiguration<CustomerEngagementData>
{
    public void Configure(EntityTypeBuilder<CustomerEngagementData> builder)
    {
        builder.HasKey(ed => ed.Id);

        builder.Property(ed => ed.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(ed => ed.CustomerId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(ed => ed.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ed => ed.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ed => ed.IsPrimarySource)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ed => ed.SourcePriority)
            .HasDefaultValue(0);

        builder.Property(ed => ed.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ed => ed.ImportBatchId)
            .HasMaxLength(50);

        builder.Property(ed => ed.ImportedByUserId)
            .HasMaxLength(36);

        // Login/Session data
        builder.Property(ed => ed.LastLoginDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(ed => ed.FirstLoginDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(ed => ed.WeeklyLoginFrequency)
            .HasDefaultValue(0);

        builder.Property(ed => ed.MonthlyLoginFrequency)
            .HasDefaultValue(0);

        builder.Property(ed => ed.TotalSessions)
            .HasDefaultValue(0);

        builder.Property(ed => ed.AverageSessionDuration)
            .HasColumnType("decimal(8,2)")  // Minutes with 2 decimal places
            .HasDefaultValue(0);

        // Feature usage
        builder.Property(ed => ed.FeatureUsagePercentage)
            .HasColumnType("decimal(5,2)")  // 0.00 to 100.00 percentage
            .HasDefaultValue(0);

        builder.Property(ed => ed.FeatureUsageCounts)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions)null) : null);

        builder.Property(ed => ed.LastFeatureUsage)
            .HasColumnType("timestamp with time zone");

        // Behavioral metrics
        builder.Property(ed => ed.PageViews)
            .HasDefaultValue(0);

        builder.Property(ed => ed.BounceRate)
            .HasColumnType("decimal(5,4)")  // 0.0000 to 1.0000 (0% to 100%)
            .HasDefaultValue(0);

        builder.Property(ed => ed.MostVisitedPages)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

        // Custom events - flexible JSON storage
        builder.Property(ed => ed.CustomEvents)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions)null) : null);

        // Sync metadata
        builder.Property(ed => ed.LastSyncedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(ed => ed.SyncVersion)
            .HasMaxLength(50);

        // Indexes for performance and analytics
        builder.HasIndex(ed => ed.CustomerId)
            .HasDatabaseName("ix_customer_engagement_data_customer_id");

        builder.HasIndex(ed => new { ed.CustomerId, ed.Source })
            .HasDatabaseName("ix_customer_engagement_data_customer_source");

        builder.HasIndex(ed => new { ed.Source, ed.ExternalId })
            .HasDatabaseName("ix_customer_engagement_data_source_external_id");

        builder.HasIndex(ed => ed.ImportBatchId)
            .HasDatabaseName("ix_customer_engagement_data_import_batch");

        builder.HasIndex(ed => new { ed.CustomerId, ed.IsPrimarySource })
            .HasDatabaseName("ix_customer_engagement_data_customer_primary");

        builder.HasIndex(ed => ed.LastLoginDate)
            .HasDatabaseName("ix_customer_engagement_data_last_login");

        builder.HasIndex(ed => ed.WeeklyLoginFrequency)
            .HasDatabaseName("ix_customer_engagement_data_weekly_login_freq");

        builder.HasIndex(ed => ed.FeatureUsagePercentage)
            .HasDatabaseName("ix_customer_engagement_data_feature_usage");

        builder.HasIndex(ed => ed.AverageSessionDuration)
            .HasDatabaseName("ix_customer_engagement_data_avg_session_duration");

        builder.HasIndex(ed => ed.IsActive)
            .HasDatabaseName("ix_customer_engagement_data_is_active");

        // Composite indexes for common query patterns
        builder.HasIndex(ed => new { ed.CustomerId, ed.LastLoginDate })
            .HasDatabaseName("ix_customer_engagement_data_customer_last_login");

        builder.HasIndex(ed => new { ed.WeeklyLoginFrequency, ed.FeatureUsagePercentage })
            .HasDatabaseName("ix_customer_engagement_data_activity_usage");

        // Relationships
        builder.HasOne(ed => ed.Customer)
            .WithMany(c => c.EngagementDataSources)
            .HasForeignKey(ed => ed.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ed => ed.ImportedBy)
            .WithMany()
            .HasForeignKey(ed => ed.ImportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("customer_engagement_data");
    }
}