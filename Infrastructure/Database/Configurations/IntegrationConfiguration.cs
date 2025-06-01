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

            builder.Property(i => i.Type)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(i => i.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(i => i.Name)
                .IsRequired()
                .HasMaxLength(100);

            // JSON columns for configuration and credentials
            builder.Property(i => i.Configuration)
                .HasColumnType("jsonb");

            builder.Property(i => i.Credentials)
                .HasColumnType("jsonb");

            builder.Property(i => i.LastSyncError)
                .HasMaxLength(2000);

            // Relationships
            builder.HasOne(i => i.User)
                .WithMany(u => u.Integrations)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
