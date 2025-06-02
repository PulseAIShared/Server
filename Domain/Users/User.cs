

using SharedKernel;
using System.ComponentModel.DataAnnotations;

namespace Domain.Users;

public sealed class User : Entity
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Avatar { get; set; }
    public string PasswordHash { get; set; }

    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    public Guid CompanyId { get; set; }
    public bool IsCompanyOwner { get; set; } 

    // Navigation properties
    public Company Company { get; set; }
    public ICollection<CompanyInvitation> SentInvitations { get; set; } = new List<CompanyInvitation>();
    public ICollection<Domain.Integration.Integration> ConfiguredIntegrations { get; set; } = new List<Domain.Integration.Integration>();

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }


    public enum UserRole
    {
        User = 0,
        CompanyOwner = 1,
        Admin = 2
    }
    public void SetRefreshToken(string token, DateTime expiryTime)
    {
        RefreshToken = token;
        RefreshTokenExpiryTime = expiryTime;
    }

    public bool IsRefreshTokenValid()
    {
        return RefreshToken != null &&
               RefreshTokenExpiryTime.HasValue &&
               RefreshTokenExpiryTime.Value > DateTime.UtcNow;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
    }
}

