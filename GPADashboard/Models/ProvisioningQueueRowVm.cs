namespace GPADashboard.Models
{
    public class ProvisioningQueueRowVm
    {
        public Guid JobId { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public string PaymentType { get; set; } = default!;

        public string OverallStatus { get; set; } = default!;     // e.g. Processing, Failed, ManualReview, Succeeded
        public string CurrentStep { get; set; } = default!;        // Payment/Policy/Billing/Receipt
        public int TotalAttempts { get; set; }                     // sum of step attempts
        public DateTime? NextRetryAtUtc { get; set; }              // min of NextRetryAt across steps
        public DateTime UpdatedAtUtc { get; set; }                 // from policy.UpdatedAt or job.UpdatedAt
        public string? LastError { get; set; }                     // from the step that failed
    }

}
