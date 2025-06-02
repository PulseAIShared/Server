using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain.Users.User;

namespace Domain.Users
{
    public class CompanyInvitation : Entity
    {
        public Guid CompanyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string InvitationToken { get; set; } = string.Empty;
        public UserRole InvitedRole { get; set; } = UserRole.User;
        public Guid InvitedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public Guid? AcceptedByUserId { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public User InvitedBy { get; set; } = null!;
        public User? AcceptedBy { get; set; }

        public bool IsValid => !IsAccepted && DateTime.UtcNow < ExpiresAt;

        public static CompanyInvitation Create(
            Guid companyId,
            string email,
            UserRole role,
            Guid invitedByUserId,
            int validForDays = 7)
        {
            return new CompanyInvitation
            {
                CompanyId = companyId,
                Email = email.ToLower(),
                InvitationToken = GenerateSecureToken(),
                InvitedRole = role,
                InvitedByUserId = invitedByUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(validForDays)
            };
        }

        private static string GenerateSecureToken()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public void Accept(Guid userId)
        {
            IsAccepted = true;
            AcceptedAt = DateTime.UtcNow;
            AcceptedByUserId = userId;
        }
    }
}