using Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Database.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Domain)
                .HasMaxLength(100);

            builder.Property(c => c.Size)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Industry)
                .HasMaxLength(100);

            // Indexes
            builder.HasIndex(c => c.Name)
                .HasDatabaseName("ix_companies_name");

            builder.HasIndex(c => c.Domain)
                .IsUnique()
                .HasDatabaseName("ix_companies_domain");

            // Relationships
            builder.HasMany(c => c.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.Customers)
                .WithOne(cu => cu.Company)
                .HasForeignKey(cu => cu.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Segments)
                .WithOne(s => s.Company)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table configuration
            builder.ToTable("companies");
        }
    }

}
