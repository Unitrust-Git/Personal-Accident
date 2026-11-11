using GPA.Application.Common;
using GPA.Application.DTO;
using GPA.Application.Interface;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence.Interface;

namespace GPA.Application.Implementation
{


    public class PolicyProvisioningJobReadService : IPolicyProvisioningJobReadService
    {

        private readonly IPolicyProvisioningJobReadRepository _repo;
        private readonly IPolicyRepository _policyRepo;

        public PolicyProvisioningJobReadService(
            IPolicyProvisioningJobReadRepository repo,
            IPolicyRepository policyRepo)
        {
            _repo = repo;
            _policyRepo = policyRepo;
        }

        public async Task<PolicyProvisioningJobDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(id, ct);
            if (entity == null) return null;

            return await MapWithPolicyAsync(entity, ct);
        }

        public async Task<PagedResult<PolicyProvisioningJobDto>> GetPagedAsync(
            ProvisioningStatus? status,
            string? paymentType,
            DateTime? dueBefore,
            Guid? paymentTransactionId,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            var (items, total) = await _repo.GetPagedAsync(
                status, paymentType, dueBefore, paymentTransactionId, search, page, pageSize, ct);

            var mapped = new List<PolicyProvisioningJobDto>(items.Count);

            foreach (var job in items)
            {
                mapped.Add(await MapWithPolicyAsync(job, ct));
            }


            return new PagedResult<PolicyProvisioningJobDto>
            {
                Items = mapped,
                TotalCount = total,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100)
            };
        }


        public async Task<PolicyProvisioningJobDto?> GetByPaymentTransactionAsync(
            Guid paymentTransactionId,
            string paymentType,
            CancellationToken ct)
        {
            var entity = await _repo.GetByPaymentTransactionAsync(paymentTransactionId, paymentType, ct);
            if (entity == null) return null;

            return await MapWithPolicyAsync(entity, ct);
        }



        private static PolicyProvisioningJobDto Map(PolicyProvisioningJob x) => new()
        {
            Id = x.Id,
            PaymentTransactionId = x.PaymentTransactionId,
            PaymentType = x.PaymentType,
            Status = x.Status,
            AttemptCount = x.AttemptCount,
            NextAttemptAt = x.NextAttemptAt,
            LastAttemptAt = x.LastAttemptAt,
            LastError = x.LastError,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };


        private static (string overall, string step, int totalAttempts, DateTime? nextRetry, string? lastError)
        Summarize(PolicyBase p)
        {
            var steps = new[]
            {
                ("Policy", p.PolicyStep),
                ("Billing", p.BillingStep),
                ("Receipt", p.ReceiptStep)
            };

            var current = steps.FirstOrDefault(s => s.Item2?.Status != WorkflowStepStatus.Succeeded);
            var currentStep = current.Item2 == null ? "Unknown" : current.Item1;

            var nextRetry = steps
                .Select(s => s.Item2?.NextRetryAt)
                .Where(d => d.HasValue)
                .Min();

            var totalAttempts = steps.Sum(s => s.Item2?.AttemptCount ?? 0);

            var err = steps
                .Select(s => s.Item2)
                .FirstOrDefault(x => x?.Status == WorkflowStepStatus.Failed || x?.Status == WorkflowStepStatus.RequiresManualReview)
                ?.LastError;

            var overall =
                steps.All(s => s.Item2?.Status == WorkflowStepStatus.Succeeded) ? "Succeeded" :
                steps.Any(s => s.Item2?.Status == WorkflowStepStatus.RequiresManualReview) ? "ManualReview" :
                steps.Any(s => s.Item2?.Status == WorkflowStepStatus.InProgress) ? "Processing" :
                steps.Any(s => s.Item2?.Status == WorkflowStepStatus.Failed) ? "Failed" :
                "Pending";

            return (overall, currentStep, totalAttempts, nextRetry, err);
        }

        private async Task<PolicyProvisioningJobDto> MapWithPolicyAsync(PolicyProvisioningJob job, CancellationToken ct)
        {
            var dto = Map(job);

            // If your repo returns PolicyBase? for any payment transaction id, this is enough.
            // Ideally you should add ct support in the repo too.
            var policy = await _policyRepo.GetByPaymentTransactionIdAsync(job.PaymentTransactionId);

            if (policy != null)
            {
                var s = Summarize(policy);
                dto.OverallStatus = s.overall;
                dto.CurrentStep = s.step;
                dto.TotalStepAttempts = s.totalAttempts;
                dto.NextRetryAtUtc = s.nextRetry;
                dto.StepLastError = s.lastError;
            }

            return dto;
        }


    }

}
