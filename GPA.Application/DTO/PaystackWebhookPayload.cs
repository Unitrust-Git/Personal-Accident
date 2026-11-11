using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class PaystackWebhookPayload
    {
        public string Event { get; set; } = default!;
        public PaystackPaymentData Data { get; set; } = default!;
    }

    public class PaystackPaymentData
    {
        public string Reference { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = default!;
        public string Email { get; set; } = default!;
        public PaymentMetadata Metadata { get; set; } = default!;
        // Add other fields from Paystack if needed
    }

}
