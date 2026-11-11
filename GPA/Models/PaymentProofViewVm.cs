namespace GPA.Models
{
    public class PaymentProofViewVm
    {
        public string Reference { get; set; } = default!;
        public string TransactionReference { get; set; } = default!;

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }

        public string PaymentType { get; set; } = "individual";

        public string? FailureReason { get; set; }

        public bool IsProofOnly => true;

        public string WatermarkText =>
            "PAYMENT CONFIRMATION ONLY - NOT A VALID INSURANCE CERTIFICATE";
    }
}
