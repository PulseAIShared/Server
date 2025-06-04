using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Import
{
    public sealed class ImportUpdateResponse
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public List<FieldUpdateResponse> UpdatedFields { get; set; } = new();
        public DateTime UpdateTime { get; set; }
    }

    public sealed class FieldUpdateResponse
    {
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }
}
