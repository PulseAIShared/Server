using Domain.Notification;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Database.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(n => n.UserId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(n => n.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(n => n.Category)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(n => n.ActionUrl)
                .HasMaxLength(500);

            builder.Property(n => n.ActionText)
                .HasMaxLength(100);

            builder.Property(n => n.Metadata)
                .HasColumnType("jsonb");

            builder.Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(n => n.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(n => n.ReadAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(n => n.ExpiresAt)
                .HasColumnType("timestamp with time zone");

            // Indexes for performance
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("ix_notifications_user_id");

            builder.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("ix_notifications_created_at");

            builder.HasIndex(n => n.IsRead)
                .HasDatabaseName("ix_notifications_is_read");

            builder.HasIndex(n => n.Category)
                .HasDatabaseName("ix_notifications_category");

            builder.HasIndex(n => new { n.UserId, n.IsRead })
                .HasDatabaseName("ix_notifications_user_read");

            builder.HasIndex(n => new { n.UserId, n.CreatedAt })
                .HasDatabaseName("ix_notifications_user_created");

            builder.HasIndex(n => n.ExpiresAt)
                .HasDatabaseName("ix_notifications_expires_at");

            // Relationships
            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Table configuration
            builder.ToTable("notifications");
        }
    }
}
