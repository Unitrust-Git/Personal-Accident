using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;

namespace GPA.Infrastructure.Persistence.Implementation
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ApplicationDbContext _context;

        public PolicyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyBase?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId)
        {
            var corporatePolicy = await _context.CorporatePolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentTransactionId == paymentTransactionId);

            if (corporatePolicy != null)
                return corporatePolicy;

            var individualPolicy = await _context.IndividualPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentTransactionId == paymentTransactionId);

            return individualPolicy;
        }

        // ✅ Tracked versions (needed for workflow updates)
        public Task<IndividualPolicy?> GetIndividualTrackedAsync(Guid paymentTransactionId, CancellationToken ct = default) =>
            _context.IndividualPolicies.FirstOrDefaultAsync(p => p.PaymentTransactionId == paymentTransactionId, ct);

        public Task<CorporatePolicy?> GetCorporateTrackedAsync(Guid paymentTransactionId, CancellationToken ct = default) =>
            _context.CorporatePolicies.FirstOrDefaultAsync(p => p.PaymentTransactionId == paymentTransactionId, ct);

        // ✅ Fix: don't use Set<PolicyBase>() unless you mapped inheritance explicitly
        public async Task<bool> ExistsForPaymentAsync(Guid paymentTransactionId, CancellationToken ct = default)
        {
            return await _context.CorporatePolicies.AnyAsync(p => p.PaymentTransactionId == paymentTransactionId, ct)
                || await _context.IndividualPolicies.AnyAsync(p => p.PaymentTransactionId == paymentTransactionId, ct);
        }

        public async Task AddIndividualAsync(IndividualPolicy policy, CancellationToken ct = default)
        {
            await _context.IndividualPolicies.AddAsync(policy, ct);
        }

        public async Task AddCorporateAsync(CorporatePolicy policy, CancellationToken ct = default)
        {
            await _context.CorporatePolicies.AddAsync(policy, ct);
        }

        public async Task SaveAsync(PolicyBase policy, CancellationToken ct = default)
        {

            var entry = _context.Entry(policy);
            if (entry.State == EntityState.Detached)
                _context.Attach(policy);

            entry.State = EntityState.Modified;

            await _context.SaveChangesAsync(ct);
        }


        // ✅ Lease lock (atomic) - SQL Server safe
        // Note: this assumes your table name is IndividualPolicies OR CorporatePolicies.
        // Because policyId could belong to either, we attempt both.
        public async Task<bool> TryAcquireLeaseAsync(Guid policyId, string lockedBy, Guid lockToken, TimeSpan leaseDuration, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var until = now.Add(leaseDuration);

            var rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE IndividualPolicies
                SET LockedBy = {lockedBy},
                    LockedUntilUtc = {until},
                    LockToken = {lockToken},
                    UpdatedAt = {now}
                WHERE Id = {policyId}
                  AND (LockedUntilUtc IS NULL OR LockedUntilUtc < {now});", ct);

            if (rows == 1) return true;

            rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE CorporatePolicies
                SET LockedBy = {lockedBy},
                    LockedUntilUtc = {until},
                    LockToken = {lockToken},
                    UpdatedAt = {now}
                WHERE Id = {policyId}
                  AND (LockedUntilUtc IS NULL OR LockedUntilUtc < {now});", ct);

            return rows == 1;
        }

        public async Task ReleaseLeaseAsync(Guid policyId, Guid lockToken, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;


            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE IndividualPolicies
                SET LockedBy = NULL,
                    LockedUntilUtc = NULL,
                    LockToken = NULL,
                    UpdatedAt = {now}
                WHERE Id = {policyId}
                  AND LockToken = {lockToken};", ct);

            // Release CorporatePolicies
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE CorporatePolicies
                SET LockedBy = NULL,
                    LockedUntilUtc = NULL,
                    LockToken = NULL,
                    UpdatedAt = {now}
                WHERE Id = {policyId}
                  AND LockToken = {lockToken};", ct);
        }

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _context.SaveChangesAsync(ct);
    }
}
