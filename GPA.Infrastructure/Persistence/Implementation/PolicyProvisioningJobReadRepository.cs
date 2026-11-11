using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;


namespace GPA.Infrastructure.Persistence.Implementation
{
    public class PolicyProvisioningJobReadRepository : IPolicyProvisioningJobReadRepository
    {
        private readonly ApplicationDbContext _context;

        public PolicyProvisioningJobReadRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<PolicyProvisioningJob?> GetByIdAsync(Guid id, CancellationToken ct)
            => _context.Set<PolicyProvisioningJob>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<(IReadOnlyList<PolicyProvisioningJob> Items, int TotalCount)> GetPagedAsync(
            ProvisioningStatus? status,
            string? paymentType,
            DateTime? dueBefore,
            Guid? paymentTransactionId,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            IQueryable<PolicyProvisioningJob> q = _context.Set<PolicyProvisioningJob>().AsNoTracking();

            if (status.HasValue)
                q = q.Where(x => x.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(paymentType))
                q = q.Where(x => x.PaymentType == paymentType.Trim());

            if (dueBefore.HasValue)
                q = q.Where(x => x.NextAttemptAt <= dueBefore.Value);

            if (paymentTransactionId.HasValue)
                q = q.Where(x => x.PaymentTransactionId == paymentTransactionId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x =>
                    x.PaymentType.Contains(s) ||
                    (x.LastError != null && x.LastError.Contains(s)));
            }

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderBy(x => x.Status)                 
                .ThenBy(x => x.NextAttemptAt)           
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<PolicyProvisioningJob?> GetByPaymentTransactionAsync(
            Guid paymentTransactionId,
            string paymentType,
            CancellationToken ct)
        {
            return await _context.Set<PolicyProvisioningJob>()
                .AsNoTracking()
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync(x =>
                    x.PaymentTransactionId == paymentTransactionId &&
                    x.PaymentType == paymentType,
                    ct);
        }


        public async Task<ProvisioningStatus?> GetStatusAsync(
            Guid paymentTransactionId,
            string paymentType,
            CancellationToken ct)
        {
            return await _context.Set<PolicyProvisioningJob>()
                .Where(x =>
                    x.PaymentTransactionId == paymentTransactionId &&
                    x.PaymentType == paymentType)
                .Select(x => (ProvisioningStatus?)x.Status)
                .FirstOrDefaultAsync(ct);
        }


    }

}
