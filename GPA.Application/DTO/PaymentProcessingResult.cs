using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class PaymentProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public Exception? Exception { get; set; }
    }

}
