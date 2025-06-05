using Domain.Customers;
using SharedKernel;
using System.ComponentModel.DataAnnotations;


namespace Domain.Customers;
public sealed class CustomerCrmData : Entity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public string Source { get; set; } = string.Empty; // salesforce, hubspot, manual_import, etc.

    [Required]
    public string ExternalId { get; set; } = string.Empty;

    public bool IsPrimarySource { get; set; } = false;
    public int SourcePriority { get; set; } = 0; // For conflict resolution

    // CRM-specific fields
    public string? LeadSource { get; set; }
    public string? LifecycleStage { get; set; }
    public string? LeadStatus { get; set; }
    public string? SalesOwnerId { get; set; }
    public string? SalesOwnerName { get; set; }
    public DateTime? FirstContactDate { get; set; }
    public DateTime? LastContactDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int DealCount { get; set; }
    public decimal TotalDealValue { get; set; }
    public decimal WonDealValue { get; set; }

    // Import/sync metadata
    public string? ImportBatchId { get; set; } // For manual imports
    public Guid? ImportedByUserId { get; set; } // Who imported manually
    public string? SyncVersion { get; set; }
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true; // Can deactivate old sources

    // Custom fields from CRM (JSON)
    public Dictionary<string, object>? CustomFields { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Domain.Users.User? ImportedBy { get; set; }
}