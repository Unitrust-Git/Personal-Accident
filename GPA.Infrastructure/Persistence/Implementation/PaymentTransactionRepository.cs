using GPA.Core.Enums;
using GPA.Core.Models;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Implementation
{
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task AddAsync(PaymentTransaction tx, CancellationToken ct)
        {
            await _context.PaymentTransactions.AddAsync(tx, ct);
        }

        public Task<PaymentTransaction?> GetByReferenceAsync(string reference, CancellationToken ct)
        {
            return _context.PaymentTransactions
                .FirstOrDefaultAsync(x => x.Reference == reference, ct);
        }
        public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return _context.PaymentTransactions
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }


        public Task UpdateAsync(PaymentTransaction tx, CancellationToken ct)
        {
            _context.PaymentTransactions.Update(tx);
            return Task.CompletedTask;
        }

        public async Task<(IReadOnlyList<PaymentTransaction> Items, int TotalCount)> GetPagedAsync(
            PaymentStatus? status,
            DateTime? from,
            DateTime? to,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

            IQueryable<PaymentTransaction> query =
                _context.PaymentTransactions.AsNoTracking();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (from.HasValue)
                query = query.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
            {
                var endExclusive = to.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < endExclusive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x =>
                    x.Reference.Contains(s) ||
                    (x.GatewayReference != null && x.GatewayReference.Contains(s)) ||
                    x.InvoiceId.Contains(s));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }

}
