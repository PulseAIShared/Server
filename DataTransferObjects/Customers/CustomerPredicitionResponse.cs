using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Customers
{
    public sealed class ChurnPredictionResponse
    {
        public Guid Id { get; set; }
        public decimal RiskScore { get; set; }
        public ChurnRiskLevel RiskLevel { get; set; }
        public DateTime PredictionDate { get; set; }
        public Dictionary<string, decimal>? RiskFactors { get; set; }
        public string? ModelVersion { get; set; }
    }
}
