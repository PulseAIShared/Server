using Domain.Imports;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Database.Configurations
{
    public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
    {
        public void Configure(EntityTypeBuilder<ImportJob> builder)
        {
            builder.HasKey(ij => ij.Id);

            builder.Property(ij => ij.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(ij => ij.UserId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(ij => ij.CompanyId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(ij => ij.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(ij => ij.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ij => ij.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(ImportJobStatus.Pending);

            builder.Property(ij => ij.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(ij => ij.ImportSource)
                .HasMaxLength(50);

            // Progress tracking fields
            builder.Property(ij => ij.TotalRecords)
                .HasDefaultValue(0);

            builder.Property(ij => ij.ProcessedRecords)
                .HasDefaultValue(0);

            builder.Property(ij => ij.SuccessfulRecords)
                .HasDefaultValue(0);

            builder.Property(ij => ij.FailedRecords)
                .HasDefaultValue(0);

            builder.Property(ij => ij.SkippedRecords)
                .HasDefaultValue(0);

            // Error handling
            builder.Property(ij => ij.ErrorMessage)
                .HasMaxLength(2000);

            builder.Property(ij => ij.ValidationErrors)
                .HasColumnType("text"); // JSON string for validation errors

            // Timestamps
            builder.Property(ij => ij.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(ij => ij.StartedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(ij => ij.CompletedAt)
                .HasColumnType("timestamp with time zone");

            // Results
            builder.Property(ij => ij.ImportSummary)
                .HasColumnType("text"); // JSON string for import insights

            // Indexes for performance
            builder.HasIndex(ij => ij.UserId)
                .HasDatabaseName("ix_import_jobs_user_id");

            builder.HasIndex(ij => ij.CompanyId)
                .HasDatabaseName("ix_import_jobs_company_id");

            builder.HasIndex(ij => ij.Status)
                .HasDatabaseName("ix_import_jobs_status");

            builder.HasIndex(ij => ij.Type)
                .HasDatabaseName("ix_import_jobs_type");

            builder.HasIndex(ij => ij.CreatedAt)
                .HasDatabaseName("ix_import_jobs_created_at");

            builder.HasIndex(ij => new { ij.UserId, ij.Status })
                .HasDatabaseName("ix_import_jobs_user_status");

            builder.HasIndex(ij => new { ij.Status, ij.CreatedAt })
                .HasDatabaseName("ix_import_jobs_status_created");

            // Relationships
            builder.HasOne(ij => ij.User)
                .WithMany()
                .HasForeignKey(ij => ij.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ij => ij.Company)
                .WithMany()
                .HasForeignKey(ij => ij.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table configuration
            builder.ToTable("import_jobs");
        }
    }
}
