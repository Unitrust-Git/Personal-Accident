using System.ComponentModel.DataAnnotations;

namespace GPA.Core.Models.Policy
{
    public enum ProvisioningStatus
    {
        Pending = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        PermanentFailed = 4
    }

    public class PolicyProvisioningJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PaymentTransactionId { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; } = default!;

        public string PaymentType { get; set; } = default!;

        public ProvisioningStatus Status { get; set; } = ProvisioningStatus.Pending;

        public int AttemptCount { get; set; }
        public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

        public string? LastError { get; set; }
        public DateTime? LastAttemptAt { get; set; }

        public string? LockedBy { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public Guid? LockToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;
    }
}
