using Domain.Campaigns;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Customers;
using SharedKernel.Enums;

namespace Infrastructure.Database.Configurations
{
    public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
    {
   
            public void Configure(EntityTypeBuilder<Campaign> builder)
            {
                builder.HasKey(c => c.Id);

                builder.Property(c => c.Id)
                    .IsRequired()
                    .HasMaxLength(36);

                builder.Property(c => c.CompanyId)
                    .IsRequired()
                    .HasMaxLength(36);

                builder.Property(c => c.SegmentId)
                    .HasMaxLength(36);

                builder.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                builder.Property(c => c.Description)
                    .HasMaxLength(1000);

                builder.Property(c => c.Type)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(20);

                builder.Property(c => c.Status)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue(CampaignStatus.Draft);

                builder.Property(c => c.Trigger)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(30);

                builder.Property(c => c.ScheduledDate)
                    .HasColumnType("timestamp");

                builder.Property(c => c.SentDate)
                    .HasColumnType("timestamp");

                // Performance metrics
                builder.Property(c => c.SentCount)
                    .HasDefaultValue(0);

                builder.Property(c => c.OpenedCount)
                    .HasDefaultValue(0);

                builder.Property(c => c.ClickedCount)
                    .HasDefaultValue(0);

                builder.Property(c => c.ConvertedCount)
                    .HasDefaultValue(0);

                builder.Property(c => c.RevenueRecovered)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0);

                // Indexes
                builder.HasIndex(c => c.CompanyId)
                    .HasDatabaseName("ix_campaigns_company_id");

                builder.HasIndex(c => c.SegmentId)
                    .HasDatabaseName("ix_campaigns_segment_id");

                builder.HasIndex(c => c.Status)
                    .HasDatabaseName("ix_campaigns_status");

                builder.HasIndex(c => c.Type)
                    .HasDatabaseName("ix_campaigns_type");

                builder.HasIndex(c => c.ScheduledDate)
                    .HasDatabaseName("ix_campaigns_scheduled_date");

                // Relationships
                builder.HasOne(c => c.Company)
                    .WithMany()
                    .HasForeignKey(c => c.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(c => c.Segment)
                    .WithMany(s => s.Campaigns)
                    .HasForeignKey(c => c.SegmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                builder.HasMany(c => c.Steps)
                    .WithOne(cs => cs.Campaign)
                    .HasForeignKey(cs => cs.CampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Many-to-many relationship with customers
                builder.HasMany(c => c.Customers)
                    .WithMany(cu => cu.Campaigns)
                    .UsingEntity<Dictionary<string, object>>(
                        "campaign_customers",
                        j => j.HasOne<Customer>().WithMany().HasForeignKey("customer_id"),
                        j => j.HasOne<Campaign>().WithMany().HasForeignKey("campaign_id"),
                        j =>
                        {
                            j.HasKey("campaign_id", "customer_id");
                            j.HasIndex("campaign_id").HasDatabaseName("ix_campaign_customers_campaign_id");
                            j.HasIndex("customer_id").HasDatabaseName("ix_campaign_customers_customer_id");
                        });

                // Table configuration
                builder.ToTable("campaigns");
            }
        }

        public class CampaignStepConfiguration : IEntityTypeConfiguration<CampaignStep>
        {
            public void Configure(EntityTypeBuilder<CampaignStep> builder)
            {
                builder.HasKey(cs => cs.Id);

                builder.Property(cs => cs.Id)
                    .IsRequired()
                    .HasMaxLength(36);

                builder.Property(cs => cs.CampaignId)
                    .IsRequired()
                    .HasMaxLength(36);

                builder.Property(cs => cs.StepOrder)
                    .IsRequired();

                builder.Property(cs => cs.Type)
                    .HasConversion<string>()
                    .IsRequired()
                    .HasMaxLength(20);

                builder.Property(cs => cs.Delay)
                    .IsRequired();

                builder.Property(cs => cs.Subject)
                    .IsRequired()
                    .HasMaxLength(300);

                builder.Property(cs => cs.Content)
                    .IsRequired()
                    .HasColumnType("text");

                // Performance metrics
                builder.Property(cs => cs.SentCount)
                    .HasDefaultValue(0);

                builder.Property(cs => cs.OpenedCount)
                    .HasDefaultValue(0);

                builder.Property(cs => cs.ClickedCount)
                    .HasDefaultValue(0);

                builder.Property(cs => cs.ConvertedCount)
                    .HasDefaultValue(0);

                // Indexes
                builder.HasIndex(cs => cs.CampaignId)
                    .HasDatabaseName("ix_campaign_steps_campaign_id");

                builder.HasIndex(cs => new { cs.CampaignId, cs.StepOrder })
                    .IsUnique()
                    .HasDatabaseName("ix_campaign_steps_campaign_order");

                // Relationships
                builder.HasOne(cs => cs.Campaign)
                    .WithMany(c => c.Steps)
                    .HasForeignKey(cs => cs.CampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Table configuration
                builder.ToTable("campaign_steps");
            }
        }


    }
