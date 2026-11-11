using GPA.Application.Interface;
using GPA.Core.Models;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GPA.Application.Implementation
{
    public class PolicyProvisioningWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PolicyProvisioningWorker> _logger;

        private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan JobLeaseDuration = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan StuckJobGrace = TimeSpan.FromMinutes(1);

        private const int MaxJobAttempts = 30;

        public PolicyProvisioningWorker(IServiceScopeFactory scopeFactory, ILogger<PolicyProvisioningWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var policyCreation = scope.ServiceProvider.GetRequiredService<IPolicyCreationService>();

                    var now = DateTime.UtcNow;

                    var claim = await TryClaimNextJobAsync(db, workerId, now, stoppingToken);
                    if (claim is null)
                    {
                        await Task.Delay(PollDelay, stoppingToken);
                        continue;
                    } 

                    var (jobId, lockToken) = claim.Value;

                    var job = await db.PolicyProvisioningJobs
                        .FirstOrDefaultAsync(j => j.Id == jobId, stoppingToken);

                    if (job == null)
                        continue;

                    job.LockToken = lockToken;

                    if (job.LockToken != lockToken)
                        continue;

                    var processingPersisted = false;

                    try
                    {
                        await MarkProcessingAsync(db, job, now, stoppingToken);

                        processingPersisted = true;

                        await ProcessJobAsync(db, policyCreation, job, stoppingToken);

                        processingPersisted = false;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await MarkJobFailedAsync(db, job, ex, stoppingToken);

                        processingPersisted = false;

                        _logger.LogError(ex,
                            "Policy provisioning failed. JobId={JobId} PaymentTransactionId={TxId} Ref={Ref}",
                            job.Id, job.PaymentTransactionId, txRefSafe(job, scope.ServiceProvider));
                    }
                    finally
                    {
                        if (processingPersisted)
                            await BestEffortReleaseLeaseAsync(db, job, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provisioning worker loop failed");
                    await Task.Delay(PollDelay, stoppingToken);
                }
            }
        }


        private static async Task MarkProcessingAsync(
            ApplicationDbContext db,
            PolicyProvisioningJob job,
            DateTime now,
            CancellationToken ct)
        {
            job.Status = ProvisioningStatus.Processing;
            job.LastAttemptAt = now;
            job.AttemptCount += 1;
            job.UpdatedAt = now;

            await db.SaveChangesAsync(ct);
        }

        private async Task ProcessJobAsync(
            ApplicationDbContext db,
            IPolicyCreationService policyCreation,
            PolicyProvisioningJob job,
            CancellationToken ct)
        {
            var tx = await db.PaymentTransactions
                .AsNoTracking()
                .FirstAsync(t => t.Id == job.PaymentTransactionId, ct);

            PolicyBase policy =
                job.PaymentType == "individual"
                    ? await policyCreation.CreateIndividualPolicyAsync(tx, ct)
                    : job.PaymentType == "corporate"
                        ? await policyCreation.CreateCorporatePolicyAsync(tx, ct)
                        : throw new InvalidOperationException($"Unknown PaymentType '{job.PaymentType}'");

            var (isDone, requiresManualReview, hasFailed, nextAttempt) = ComputeJobOutcomeFromPolicy(policy);
            var bestErr = GetBestPolicyError(policy);
            var now2 = DateTime.UtcNow;

            if (requiresManualReview)
            {
                job.Status = ProvisioningStatus.PermanentFailed;
                job.LastError = Truncate(bestErr ?? "Workflow requires manual review (step exceeded max attempts).", 500);
                job.NextAttemptAt = DateTime.MaxValue;
            }
            else if (isDone)
            {
                job.Status = ProvisioningStatus.Succeeded;
                job.LastError = null;
                job.NextAttemptAt = DateTime.MaxValue;
            }
            else if (hasFailed)
            {

                job.Status = ProvisioningStatus.Failed;

                if (!string.IsNullOrWhiteSpace(bestErr))
                    job.LastError = Truncate(bestErr, 500);


                job.NextAttemptAt = nextAttempt ?? now2.AddMinutes(1);
            }
            else
            {
                job.Status = ProvisioningStatus.Pending;

                if (!string.IsNullOrWhiteSpace(bestErr))
                    job.LastError = Truncate(bestErr, 500);

                job.NextAttemptAt = nextAttempt ?? now2.AddMinutes(1);
            }

            job.UpdatedAt = now2;

            if (job.AttemptCount >= MaxJobAttempts && job.Status != ProvisioningStatus.Succeeded)
            {
                job.Status = ProvisioningStatus.PermanentFailed;
                job.LastError = Truncate($"Exceeded MaxJobAttempts={MaxJobAttempts}. LastError={job.LastError}", 500);
                job.NextAttemptAt = DateTime.MaxValue;
            }

            ReleaseJobLease(job, now2);
            await db.SaveChangesAsync(ct);
        }

        private static async Task MarkJobFailedAsync(ApplicationDbContext db,PolicyProvisioningJob job,
            Exception ex,CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            job.LastError = Truncate(ex.Message, 500);
            job.Status = job.AttemptCount >= MaxJobAttempts
                ? ProvisioningStatus.PermanentFailed
                : ProvisioningStatus.Failed;

            job.NextAttemptAt = job.Status == ProvisioningStatus.PermanentFailed
                ? DateTime.MaxValue
                : now.AddMinutes(1);

            job.UpdatedAt = now;

            ReleaseJobLease(job, now);
            await db.SaveChangesAsync(ct);
        }

        private async Task BestEffortReleaseLeaseAsync(
            ApplicationDbContext db,
            PolicyProvisioningJob job,
            CancellationToken ct)
        {
            try
            {
                var now = DateTime.UtcNow;

                
                job.Status = job.Status == ProvisioningStatus.Processing ? ProvisioningStatus.Failed : job.Status;
                job.LastError ??= "Worker exited unexpectedly; lease released by safety-net.";
                job.NextAttemptAt = job.NextAttemptAt == default ? now.AddMinutes(1) : job.NextAttemptAt;
                job.UpdatedAt = now;

                ReleaseJobLease(job, now);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx, "Failed to release lease in finally. JobId={JobId}", job.Id);
            }
        }


        private static async Task<(Guid jobId, Guid lockToken)?> TryClaimNextJobAsync(
            ApplicationDbContext db,
            string workerId,
            DateTime now,
            CancellationToken ct)
        {
            var lockToken = Guid.NewGuid();
            var until = now.Add(JobLeaseDuration);
            var stuckBefore = now - StuckJobGrace;

            var sql = $@"
                ;WITH cte AS (
                    SELECT TOP(1) *
                    FROM PolicyProvisioningJobs WITH (ROWLOCK, READPAST, UPDLOCK)
                    WHERE
                        Status NOT IN ({(int)ProvisioningStatus.Succeeded}, {(int)ProvisioningStatus.PermanentFailed})
                        AND (NextAttemptAt IS NULL OR NextAttemptAt <= @p_now)
                        AND (
                            LockedUntilUtc IS NULL
                            OR LockedUntilUtc < @p_now
                            OR (Status = {(int)ProvisioningStatus.Processing} AND LastAttemptAt IS NOT NULL AND LastAttemptAt <= @p_stuckBefore)
                        )
                    ORDER BY COALESCE(NextAttemptAt, '19000101') ASC
                )
                UPDATE cte
                SET
                    LockedBy = @p_workerId,
                    LockedUntilUtc = @p_until,
                    LockToken = @p_lockToken,
                    UpdatedAt = @p_now
                OUTPUT INSERTED.Id AS Id;
                ";

            //var sql = $@"
            //; WITH cte AS(
            //    SELECT TOP(1) *
            //    FROM PolicyProvisioningJobs WITH(ROWLOCK, READPAST, UPDLOCK)

            //    WHERE

            //        Status<> {(int)ProvisioningStatus.Succeeded}
            //ORDER BY CreatedAt ASC
            //        )
            //        UPDATE cte
            //        SET
            //            LockedBy = @p_workerId,
            //            LockedUntilUtc = @p_until,
            //            LockToken = @p_lockToken,
            //            UpdatedAt = @p_now
            //        OUTPUT INSERTED.Id AS Id;
            //";

            //var sql = $@"
            //    ;WITH cte AS(
            //        SELECT TOP(1) *
            //        FROM PolicyProvisioningJobs WITH(ROWLOCK, READPAST, UPDLOCK)
            //        WHERE
            //            Id = '59eced40-6974-412a-8894-a2bc9ea2f8ab'

            //        ORDER BY CreatedAt ASC
            //    )
            //    UPDATE cte
            //    SET
            //        LockedBy = @p_workerId,
            //        LockedUntilUtc = @p_until,
            //        LockToken = @p_lockToken,
            //        UpdatedAt = @p_now
            //    OUTPUT INSERTED.Id AS Id;
            //    ";

            var result = await db.Set<ClaimedJobRow>()
                .FromSqlRaw(
                    sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@p_now", now),
                    new Microsoft.Data.SqlClient.SqlParameter("@p_until", until),
                    new Microsoft.Data.SqlClient.SqlParameter("@p_workerId", workerId),
                    new Microsoft.Data.SqlClient.SqlParameter("@p_lockToken", lockToken),
                    new Microsoft.Data.SqlClient.SqlParameter("@p_stuckBefore", stuckBefore)
                )
                .AsNoTracking()
                .ToListAsync(ct);

            var row = result.FirstOrDefault();
            if (row == null || row.Id == Guid.Empty)
                return null;

            return (row.Id, lockToken);
        }

        private static (bool done, bool manualReview, bool hasFailed, DateTime? nextAttempt) ComputeJobOutcomeFromPolicy(PolicyBase policy)
        {
            policy.PolicyStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.BillingStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.ReceiptStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };

            bool manual =
                policy.PolicyStep.Status == WorkflowStepStatus.RequiresManualReview ||
                policy.BillingStep.Status == WorkflowStepStatus.RequiresManualReview ||
                policy.ReceiptStep.Status == WorkflowStepStatus.RequiresManualReview;

            bool done =
                policy.PolicyStep.Status == WorkflowStepStatus.Succeeded &&
                policy.BillingStep.Status == WorkflowStepStatus.Succeeded &&
                policy.ReceiptStep.Status == WorkflowStepStatus.Succeeded;

            bool hasFailed =
                policy.PolicyStep.Status == WorkflowStepStatus.Failed ||
                policy.BillingStep.Status == WorkflowStepStatus.Failed ||
                policy.ReceiptStep.Status == WorkflowStepStatus.Failed;

            var next = MinDate(
                policy.PolicyStep.NextRetryAt,
                policy.BillingStep.NextRetryAt,
                policy.ReceiptStep.NextRetryAt
            );

            return (done, manual, hasFailed, next);

        }

        private static DateTime? MinDate(params DateTime?[] dates)
        {
            DateTime? min = null;
            foreach (var d in dates)
            {
                if (d == null) continue;
                if (min == null || d.Value < min.Value) min = d.Value;
            }
            return min;
        }

        private static void ReleaseJobLease(PolicyProvisioningJob job, DateTime now)
        {
            job.LockedBy = null;
            job.LockedUntilUtc = null;
            job.LockToken = null;
            job.UpdatedAt = now;
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Unknown error";
            return s.Length <= max ? s : s[..max];
        }

        private static string? txRefSafe(PolicyProvisioningJob job, IServiceProvider sp)
        {
            try
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                return db.PaymentTransactions.AsNoTracking()
                    .Where(x => x.Id == job.PaymentTransactionId)
                    .Select(x => x.Reference)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        private static string? GetBestPolicyError(PolicyBase p)
        {
            var candidate =
                p.PolicyStep?.Status is WorkflowStepStatus.Failed or WorkflowStepStatus.RequiresManualReview
                    ? p.PolicyStep.LastError
                    : p.BillingStep?.Status is WorkflowStepStatus.Failed or WorkflowStepStatus.RequiresManualReview
                        ? p.BillingStep.LastError
                        : p.ReceiptStep?.Status is WorkflowStepStatus.Failed or WorkflowStepStatus.RequiresManualReview
                            ? p.ReceiptStep.LastError
                            : null;

            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;


            return new[]
            {
            p.ReceiptStep?.LastError,
            p.BillingStep?.LastError,
            p.PolicyStep?.LastError
        }.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

    }
}
