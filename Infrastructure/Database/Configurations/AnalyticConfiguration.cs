using Domain.Analytics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Database.Configurations
{
    public class DashboardMetricsConfiguration : IEntityTypeConfiguration<DashboardMetrics>
    {
        public void Configure(EntityTypeBuilder<DashboardMetrics> builder)
        {
            builder.HasKey(dm => dm.Id);

            builder.Property(dm => dm.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(dm => dm.CompanyId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(dm => dm.MetricDate)
                .IsRequired()
                .HasColumnType("date");

            builder.Property(dm => dm.TotalCustomers)
                .HasDefaultValue(0);

            builder.Property(dm => dm.ChurnRate)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0);

            builder.Property(dm => dm.RevenueRecovered)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(dm => dm.AverageLifetimeValue)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(dm => dm.HighRiskCustomers)
                .HasDefaultValue(0);

            builder.Property(dm => dm.ActiveCampaigns)
                .HasDefaultValue(0);

            builder.Property(dm => dm.CampaignSuccessRate)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0);

            // Indexes
            builder.HasIndex(dm => dm.CompanyId)
                .HasDatabaseName("ix_dashboard_metrics_company_id");

            builder.HasIndex(dm => dm.MetricDate)
                .HasDatabaseName("ix_dashboard_metrics_metric_date");

            builder.HasIndex(dm => new { dm.CompanyId, dm.MetricDate })
                .IsUnique()
                .HasDatabaseName("ix_dashboard_metrics_company_date");

            // Relationships
            builder.HasOne(dm => dm.Company)
                .WithMany()
                .HasForeignKey(dm => dm.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table configuration
            builder.ToTable("dashboard_metrics");
        }
    }
}
