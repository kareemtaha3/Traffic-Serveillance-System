using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend_dotnet.dto
{
    public class FeeResponse
    {
        public string Plate { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsPaid { get; set; }
        public string ViolationReason { get; set; } = string.Empty;
    }
}