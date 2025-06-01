using Domain.Customers;
using SharedKernel;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Segments
{
    public class ChurnPrediction : Entity
    {
        [Required]
        public Guid CustomerId { get; set; } = Guid.Empty;

        public decimal RiskScore { get; set; }

        public ChurnRiskLevel RiskLevel { get; set; }

        public DateTime PredictionDate { get; set; } = DateTime.UtcNow;

        public Dictionary<string, decimal>? RiskFactors { get; set; }

        public string? ModelVersion { get; set; }

        // Navigation properties
        public Customer Customer { get; set; } = null!;
    }
}
