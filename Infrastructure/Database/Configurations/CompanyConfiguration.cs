using Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel.Enums;

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

            builder.Property(c => c.Country)
                .HasMaxLength(2);

            builder.Property(c => c.OwnerId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(c => c.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

                builder.Property(c => c.Plan)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(CompanyPlan.Free);

            // Remove MaxUsers column since it's now calculated

            // Indexes
            builder.HasIndex(c => c.Name)
                .HasDatabaseName("ix_companies_name");

            builder.HasIndex(c => c.Domain)
                .IsUnique()
                .HasDatabaseName("ix_companies_domain");

            builder.HasIndex(c => c.OwnerId)
                .HasDatabaseName("ix_companies_owner_id");

            // Relationships
            builder.HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Customers)
                .WithOne(cu => cu.Company)
                .HasForeignKey(cu => cu.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Segments)
                .WithOne(s => s.Company)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Integrations)
                .WithOne(i => i.Company)
                .HasForeignKey(i => i.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("companies");
        }
    }

    public class CompanyInvitationConfiguration : IEntityTypeConfiguration<CompanyInvitation>
    {
        public void Configure(EntityTypeBuilder<CompanyInvitation> builder)
        {
            builder.HasKey(ci => ci.Id);

            builder.Property(ci => ci.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(ci => ci.CompanyId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(ci => ci.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(ci => ci.InvitationToken)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ci => ci.InvitedRole)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(ci => ci.InvitedByUserId)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(ci => ci.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(ci => ci.ExpiresAt)
                .IsRequired();

            builder.Property(ci => ci.IsAccepted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(ci => ci.AcceptedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(ci => ci.AcceptedByUserId)
                .HasMaxLength(36);

            // Indexes
            builder.HasIndex(ci => ci.InvitationToken)
                .IsUnique()
                .HasDatabaseName("ix_company_invitations_token");

            builder.HasIndex(ci => ci.Email)
                .HasDatabaseName("ix_company_invitations_email");

            builder.HasIndex(ci => ci.CompanyId)
                .HasDatabaseName("ix_company_invitations_company_id");

            builder.HasIndex(ci => ci.ExpiresAt)
                .HasDatabaseName("ix_company_invitations_expires_at");

            builder.HasIndex(ci => new { ci.Email, ci.CompanyId, ci.IsAccepted })
                .HasDatabaseName("ix_company_invitations_email_company_accepted");

            // Relationships
            builder.HasOne(ci => ci.Company)
                .WithMany(c => c.Invitations)
                .HasForeignKey(ci => ci.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ci => ci.InvitedBy)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(ci => ci.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ci => ci.AcceptedBy)
                .WithMany()
                .HasForeignKey(ci => ci.AcceptedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ToTable("company_invitations");
        }
    }


}
