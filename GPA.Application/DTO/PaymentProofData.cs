namespace GPA.Application.DTO
{
    public class PaymentProofData
    {
        public string Reference { get; set; } = default!;
        public string TransactionReference { get; set; } = default!;
        public PaymentMetadata metadata { get; set; } = new PaymentMetadata();
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
