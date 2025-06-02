using Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.Role)
                .HasConversion<string>()
                .IsRequired();

            // Make CompanyId nullable to break circular dependency
            builder.Property(u => u.CompanyId)
                .IsRequired() // Keep this required for business logic
                .HasMaxLength(36);

            builder.Property(u => u.IsCompanyOwner)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.RefreshToken)
                .HasMaxLength(500);

            builder.Property(u => u.RefreshTokenExpiryTime)
                .HasColumnType("timestamp with time zone");

            // Relationships - EXPLICIT NAVIGATION PROPERTIES
            builder.HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.SentInvitations)
                .WithOne(ci => ci.InvitedBy)
                .HasForeignKey(ci => ci.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.ConfiguredIntegrations)
                .WithOne(i => i.ConfiguredBy)
                .HasForeignKey(i => i.ConfiguredByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable("users");
        }
    }
}