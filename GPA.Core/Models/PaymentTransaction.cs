using GPA.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Models
{

    public class PaymentTransaction
    {
        public Guid Id { get; set; }

        public string Reference { get; set; } = default!;

        public string? GatewayReference { get; set; }
        public string ? AccessCode { get; set; }

        public int AmountInKobo { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NGN";

        public PaymentStatus Status { get; set; }

        public string InvoiceId { get; set; }

        public string Gateway { get; set; } = "Paystack";

        public string? Metadata { get; set; }

        public string? FailureReason { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
