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
    public class SegmentCriteria : Entity
    {
        [Required]
        public Guid SegmentId { get; set; } = Guid.Empty;

        [Required]
        public string Field { get; set; } = string.Empty;

        [Required]
        public CriteriaOperator Operator { get; set; }

        [Required]
        public string Value { get; set; } = string.Empty;

        [Required]
        public string Label { get; set; } = string.Empty;

        // Navigation properties
        public CustomerSegment Segment { get; set; } = null!;
    }
}
