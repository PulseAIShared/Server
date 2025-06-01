using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public class ImportError
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? RawData { get; set; } // Original row data for debugging
        public DateTime ErrorTime { get; set; } = DateTime.UtcNow;
    }
}
