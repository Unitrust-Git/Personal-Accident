using GPA.Core.Models;

namespace GPA.Core.Dto.Paystack
{
    public class PaystackInitRequest
    {
        public string Email { get; set; } = default!;
        public int Amount { get; set; }
        public string Reference { get; set; } = default!;
        public PaymentMetadata Metadata { get; set; } = default!;
    }

}
