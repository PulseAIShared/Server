using Domain.Customers;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Segments;

namespace Infrastructure.Database.Configurations
{
    public class SegmentConfiguration : IEntityTypeConfiguration<CustomerSegment>
    {
        public void Configure(EntityTypeBuilder<CustomerSegment> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Description)
                .HasMaxLength(1000);

            builder.Property(s => s.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(s => s.Color)
                .HasMaxLength(7)
                .HasDefaultValue("#3b82f6");

            builder.Property(s => s.AverageChurnRate)
                .HasColumnType("decimal(5,2)");

            builder.Property(s => s.AverageLifetimeValue)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.AverageRevenue)
                .HasColumnType("decimal(18,2)");

            // Relationships
            builder.HasOne(s => s.Company)
                .WithMany(c => c.Segments)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Criteria)
                .WithOne(sc => sc.Segment)
                .HasForeignKey(sc => sc.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Campaigns)
                .WithOne(c => c.Segment)
                .HasForeignKey(c => c.SegmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class SegmentCriteriaConfiguration : IEntityTypeConfiguration<SegmentCriteria>
    {
        public void Configure(EntityTypeBuilder<SegmentCriteria> builder)
        {
            builder.HasKey(sc => sc.Id);

            builder.Property(sc => sc.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(sc => sc.SegmentId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(sc => sc.Field)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sc => sc.Operator)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(sc => sc.Value)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(sc => sc.Label)
                .IsRequired()
                .HasMaxLength(200);

            // Indexes
            builder.HasIndex(sc => sc.SegmentId)
                .HasDatabaseName("ix_segment_criteria_segment_id");

            builder.HasIndex(sc => sc.Field)
                .HasDatabaseName("ix_segment_criteria_field");

            // Relationships
            builder.HasOne(sc => sc.Segment)
                .WithMany(s => s.Criteria)
                .HasForeignKey(sc => sc.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table configuration
            builder.ToTable("segment_criteria");
        }
    }
}
