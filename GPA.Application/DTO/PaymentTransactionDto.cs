using GPA.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = default!;
        public string? GatewayReference { get; set; }
        public string? InvoiceId { get; set; }

        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
