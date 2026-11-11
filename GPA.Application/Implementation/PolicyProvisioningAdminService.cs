using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;

namespace GPA.Application.Implementation
{
    public class PolicyProvisioningAdminService : IPolicyProvisioningAdminService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPolicyRepository _policyRepo;

        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

        public PolicyProvisioningAdminService(ApplicationDbContext db, IPolicyRepository policyRepo)
        {
            _db = db;
            _policyRepo = policyRepo;
        }

        public async Task ResetProvisioningAsync(Guid jobId, string scope, CancellationToken ct)
        {
            scope = (scope ?? "").Trim().ToLowerInvariant();
            if (scope is not ("policy" or "billing" or "receipt" or "all"))
                throw new InvalidOperationException("Invalid reset scope. Use policy|billing|receipt|all.");

            var job = await _db.Set<PolicyProvisioningJob>()
                .FirstOrDefaultAsync(x => x.Id == jobId, ct);

            if (job == null)
                throw new InvalidOperationException("Provisioning job not found.");

            var policy = await _policyRepo.GetByPaymentTransactionIdAsync(job.PaymentTransactionId);
            if (policy == null)
                throw new InvalidOperationException("Policy not found for this transaction.");

            var token = Guid.NewGuid();
            var acquired = await _policyRepo.TryAcquireLeaseAsync(policy.Id, "dashboard-reset", token, LeaseDuration, ct);
            if (!acquired)
                throw new InvalidOperationException("Policy is currently being processed. Try again in a minute.");

            try
            {
                EnsureResetAllowed(policy, scope);
                ApplyReset(policy, scope);

                job.Status = ProvisioningStatus.Pending;
                job.LastError = null;
                job.NextAttemptAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;

                //await _db.SaveChangesAsync(ct);
                await _policyRepo.SaveChangesAsync(ct);
            }
            finally
            {
                await _policyRepo.ReleaseLeaseAsync(policy.Id, token, ct);
            }
        }

        public async Task RetryFromFirstIncompleteStepAsync(Guid jobId, CancellationToken ct)
        {
            var job = await _db.Set<PolicyProvisioningJob>()
                .FirstOrDefaultAsync(x => x.Id == jobId, ct);

            if (job == null)
                throw new InvalidOperationException("Provisioning job not found.");

            var policy = await _policyRepo.GetByPaymentTransactionIdAsync(job.PaymentTransactionId);
            if (policy == null)
                throw new InvalidOperationException("Policy not found for this transaction.");

            EnsureSteps(policy);

            if (policy.PolicyStep.Status == WorkflowStepStatus.Succeeded &&
                policy.BillingStep.Status == WorkflowStepStatus.Succeeded &&
                policy.ReceiptStep.Status == WorkflowStepStatus.Succeeded)
            {
                throw new InvalidOperationException("Policy creation is complete");
            }

            var token = Guid.NewGuid();
            var acquired = await _policyRepo.TryAcquireLeaseAsync(policy.Id, "dashboard-retry", token, LeaseDuration, ct);

            if (!acquired)
                throw new InvalidOperationException("Policy is currently being processed. Try again in a minute.");

            try
            {
                var scope =
                    policy.PolicyStep.Status != WorkflowStepStatus.Succeeded ? "policy" :
                    policy.BillingStep.Status != WorkflowStepStatus.Succeeded ? "billing" :
                    "receipt";

                EnsureResetAllowed(policy, scope);

                ApplyReset(policy, scope);

                job.Status = ProvisioningStatus.Pending;
                job.NextAttemptAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;

                policy!.EmailStep!.Status  = WorkflowStepStatus.NotStarted;

                if (!string.IsNullOrWhiteSpace(job.LastError))
                    job.LastError = $"[RETRY {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {job.LastError}";

                await _db.SaveChangesAsync(ct);
                await _policyRepo.SaveChangesAsync(ct);
            }
            finally
            {
                await _policyRepo.ReleaseLeaseAsync(policy.Id, token, ct);
            }
        }

        private static void ApplyReset(PolicyBase policy, string scope)
        {
            EnsureSteps(policy);

            var now = DateTime.UtcNow;
            
            void ResetStep(WorkflowStepState s)
            {
                s.Status = WorkflowStepStatus.NotStarted;
                s.AttemptCount = 0;
                s.LastAttemptAt = null;
                s.SucceededAt = null;
                s.NextRetryAt = null;
                s.LastError = null;
            }

            if (scope == "all")
            {
                ResetStep(policy.PolicyStep);
                ResetStep(policy.BillingStep);
                ResetStep(policy.ReceiptStep);

                policy.PolicyNumber = null;
                policy.ItemCode = null;
                policy.DebitCreditNoteNumber = null;
                policy.ReceiptNumber = null;

                policy.UpdatedAt = now;
                return;
            }

            if (scope == "policy")
            {
                ResetStep(policy.PolicyStep);
                ResetStep(policy.BillingStep);
                ResetStep(policy.ReceiptStep);

                policy.PolicyNumber = null;
                policy.ItemCode = null;
                policy.DebitCreditNoteNumber = null;
                policy.ReceiptNumber = null;

                policy.UpdatedAt = now;
                return;
            }

            if (scope == "billing")
            {
                ResetStep(policy.BillingStep);
                ResetStep(policy.ReceiptStep);

                policy.DebitCreditNoteNumber = null;
                policy.ReceiptNumber = null;

                policy.UpdatedAt = now;
                return;
            }

            if (scope == "receipt")
            {
                ResetStep(policy.ReceiptStep);

                policy.ReceiptNumber = null;

                policy.UpdatedAt = now;
                return;
            }
        }

        private static void EnsureSteps(PolicyBase policy)
        {
            policy.PaymentStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.PolicyStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.BillingStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.ReceiptStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
        }


        private static void EnsureResetAllowed(PolicyBase policy, string scope)
        {
            policy.PolicyStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.BillingStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.ReceiptStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };

            bool policyOk = policy.PolicyStep.Status == WorkflowStepStatus.Succeeded;
            bool billingOk = policy.BillingStep.Status == WorkflowStepStatus.Succeeded;
            bool receiptOk = policy.ReceiptStep.Status == WorkflowStepStatus.Succeeded;

            if (policyOk && billingOk && receiptOk)
                throw new InvalidOperationException("Reset blocked: provisioning has already succeeded.");


            if (scope == "policy" || scope == "all")
            {
                if (policyOk)
                    throw new InvalidOperationException("Reset blocked: Policy step already succeeded.");
                return;
            }

            if (scope == "billing")
            {
                if (billingOk)
                    throw new InvalidOperationException("Reset blocked: Billing step already succeeded.");
                return;
            }

            if (scope == "receipt")
            {
                if (receiptOk)
                    throw new InvalidOperationException("Reset blocked: Receipt step already succeeded.");
                return;
            }

            throw new InvalidOperationException("Invalid reset scope.");
        }

    }
}
