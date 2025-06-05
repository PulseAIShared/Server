using Domain.Campaigns;
using Domain.Customers;
using Domain.Segments;
using Domain.Users;
using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Domain.Customers;
public sealed class Customer : Entity
{
    [Required]
    public Guid CompanyId { get; set; } = Guid.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? TimeZone { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }

    // Calculated/Aggregated fields from various sources
    public decimal ChurnRiskScore { get; set; }
    public ChurnRiskLevel ChurnRiskLevel { get; set; }
    public DateTime? ChurnPredictionDate { get; set; }

    // Master record tracking
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    public string? PrimaryCrmSource { get; set; }
    public string? PrimaryPaymentSource { get; set; }
    public string? PrimaryMarketingSource { get; set; }
    public string? PrimarySupportSource { get; set; }
    public string? PrimaryEngagementSource { get; set; }

    // Navigation properties - Now ONE-TO-MANY instead of one-to-one
    public Company Company { get; set; } = null!;
    public ICollection<CustomerActivity> Activities { get; set; } = new List<CustomerActivity>();
    public ICollection<ChurnPrediction> ChurnPredictions { get; set; } = new List<ChurnPrediction>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    // Multiple sources per data type
    public ICollection<CustomerCrmData> CrmDataSources { get; set; } = new List<CustomerCrmData>();
    public ICollection<CustomerPaymentData> PaymentDataSources { get; set; } = new List<CustomerPaymentData>();
    public ICollection<CustomerMarketingData> MarketingDataSources { get; set; } = new List<CustomerMarketingData>();
    public ICollection<CustomerSupportData> SupportDataSources { get; set; } = new List<CustomerSupportData>();
    public ICollection<CustomerEngagementData> EngagementDataSources { get; set; } = new List<CustomerEngagementData>();

    // Helper methods to get primary source data
    public CustomerCrmData? GetPrimaryCrmData() =>
        CrmDataSources.FirstOrDefault(c => c.Source == PrimaryCrmSource) ??
        CrmDataSources.OrderByDescending(c => c.LastSyncedAt).FirstOrDefault();

    public CustomerPaymentData? GetPrimaryPaymentData() =>
        PaymentDataSources.FirstOrDefault(p => p.Source == PrimaryPaymentSource) ??
        PaymentDataSources.OrderByDescending(p => p.LastSyncedAt).FirstOrDefault();

    public CustomerMarketingData? GetPrimaryMarketingData() =>
        MarketingDataSources.FirstOrDefault(m => m.Source == PrimaryMarketingSource) ??
        MarketingDataSources.OrderByDescending(m => m.LastSyncedAt).FirstOrDefault();

    public CustomerSupportData? GetPrimarySupportData() =>
        SupportDataSources.FirstOrDefault(s => s.Source == PrimarySupportSource) ??
        SupportDataSources.OrderByDescending(s => s.LastSyncedAt).FirstOrDefault();

    public CustomerEngagementData? GetPrimaryEngagementData() =>
        EngagementDataSources.FirstOrDefault(e => e.Source == PrimaryEngagementSource) ??
        EngagementDataSources.OrderByDescending(e => e.LastSyncedAt).FirstOrDefault();

    // Methods to get all sources of a specific type
    public CustomerCrmData? GetCrmDataBySource(string source) =>
        CrmDataSources.FirstOrDefault(c => c.Source.Equals(source, StringComparison.OrdinalIgnoreCase));

    public CustomerPaymentData? GetPaymentDataBySource(string source) =>
        PaymentDataSources.FirstOrDefault(p => p.Source.Equals(source, StringComparison.OrdinalIgnoreCase));

    // Method to set primary sources
    public void SetPrimaryCrmSource(string source)
    {
        if (CrmDataSources.Any(c => c.Source == source))
            PrimaryCrmSource = source;
    }

    public void SetPrimaryPaymentSource(string source)
    {
        if (PaymentDataSources.Any(p => p.Source == source))
            PrimaryPaymentSource = source;
    }
}
