using Microsoft.EntityFrameworkCore;

namespace GPA.Core.Models.Policy
{
    public abstract class PolicyBase
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string ClientCode { get; set; } = default!; 

        public Guid PaymentTransactionId { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; } = default!;


        public string? PolicyNumber { get; set; }

        public string? NaicomId { get; set; }
        public string? ItemCode { get; set; }

        public string? DebitCreditNoteNumber { get; set; }  
        public string? ReceiptNumber { get; set; }

        public WorkflowStepState PaymentStep { get; set; } = new();
        public WorkflowStepState PolicyStep { get; set; } = new();
        public WorkflowStepState BillingStep { get; set; } = new();
        public WorkflowStepState ReceiptStep { get; set; } = new();

        public WorkflowStepState? EmailStep { get; set; }

        public PolicyEmailKind EmailKind { get; set; } = PolicyEmailKind.NotDecided;
        public DateTime? EmailKindDecidedAt { get; set; }
        public DateTime? EmailSentAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string? LockedBy { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public Guid? LockToken { get; set; }

    }

    [Owned]
    public class WorkflowStepState
    {
        public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.NotStarted;

        public int AttemptCount { get; set; } = 0;

        public DateTime? LastAttemptAt { get; set; }
        public DateTime? SucceededAt { get; set; }

        public string? LastError { get; set; }

        public DateTime? NextRetryAt { get; set; }
    }


    public enum WorkflowStepStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Succeeded = 2,
        Failed = 3,
        RequiresManualReview = 4
    }

    public enum PolicyEmailKind
    {
        NotDecided = 0,
        PolicyCertificate = 1,
        PaymentProof = 2
    }

}
