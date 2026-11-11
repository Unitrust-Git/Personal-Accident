using GPA.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public sealed class PaymentDetailsDto
    {
        public Guid Id { get; init; }
        public required string Reference { get; init; }
        public string? GatewayReference { get; init; }
        public string? AccessCode { get; init; }

        public int AmountInKobo { get; init; }
        public decimal Amount { get; init; }
        public required string Currency { get; init; }

        public PaymentStatus Status { get; init; }
        public required string InvoiceId { get; init; }
        public required string Gateway { get; init; }

        public string? Metadata { get; init; }
        public string? FailureReason { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

}
