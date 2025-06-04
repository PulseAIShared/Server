using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public class ImportUpdate
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<FieldUpdate> UpdatedFields { get; set; } = new();
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    }

    public class FieldUpdate
    {
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }
}
