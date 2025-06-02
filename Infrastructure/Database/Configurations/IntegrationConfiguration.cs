using Domain.Integration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Database.Configurations
{
    public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
    {
        public void Configure(EntityTypeBuilder<Integration> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.CompanyId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(i => i.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(i => i.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(i => i.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(i => i.ConfiguredByUserId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(i => i.ConfiguredAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // JSON columns for configuration and credentials
            builder.Property(i => i.Configuration)
                .HasColumnType("jsonb");

            builder.Property(i => i.Credentials)
                .HasColumnType("jsonb");

            builder.Property(i => i.LastSyncError)
                .HasMaxLength(2000);

            // Indexes
            builder.HasIndex(i => i.CompanyId)
                .HasDatabaseName("ix_integrations_company_id");

            builder.HasIndex(i => i.Type)
                .HasDatabaseName("ix_integrations_type");

            builder.HasIndex(i => i.Status)
                .HasDatabaseName("ix_integrations_status");

            // Unique constraint: one integration of each type per company
            builder.HasIndex(i => new { i.CompanyId, i.Type })
                .IsUnique()
                .HasDatabaseName("ix_integrations_company_type");

            // Relationships
            builder.HasOne(i => i.Company)
                .WithMany(c => c.Integrations)
                .HasForeignKey(i => i.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.ConfiguredBy)
                .WithMany(u => u.ConfiguredIntegrations)
                .HasForeignKey(i => i.ConfiguredByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable("integrations");
        }
    }
}
