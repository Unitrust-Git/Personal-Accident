using GPA.Core.Models.Policy;

namespace GPA.Application.DTO
{


    public class PolicyProvisioningJobDto
    {
        public Guid Id { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public string PaymentType { get; set; } = default!;
        public ProvisioningStatus Status { get; set; }

        public int AttemptCount { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string OverallStatus { get; set; } = "Pending";
        public string CurrentStep { get; set; } = "Unknown";
        public int TotalStepAttempts { get; set; }
        public DateTime? NextRetryAtUtc { get; set; }
        public string? StepLastError { get; set; }
    }


}
