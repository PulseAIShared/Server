using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferObjects.Import
{
    public sealed class ImportErrorResponse
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? RawData { get; set; }
        public DateTime ErrorTime { get; set; }
    }
}
