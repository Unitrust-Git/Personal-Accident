using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Implementation.DasboardServices
{
    using GPA.Application.Common;
    using GPA.Application.DTO;
    using GPA.Core.Enums;
    using GPA.Core.Models;
    using GPA.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using System.Linq.Expressions;

    public class PaymentTransactionReadService : IPaymentTransactionReadService
    {
        private readonly ApplicationDbContext _context;

        public PaymentTransactionReadService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentTransactionDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.PaymentTransactions
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(MapToDto())
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PaymentTransactionDto?> GetByReferenceAsync(string reference, CancellationToken ct)
        {
            return await _context.PaymentTransactions
                .AsNoTracking()
                .Where(x => x.Reference == reference)
                .Select(MapToDto())
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResult<PaymentTransactionDto>> GetPagedAsync(
            PaymentStatus? status,
            DateTime? from,
            DateTime? to,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            IQueryable<PaymentTransaction> query =
                _context.PaymentTransactions.AsNoTracking();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (from.HasValue)
                query = query.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
            {
                if (to.Value.TimeOfDay == TimeSpan.Zero)
                {
                    var endExclusive = to.Value.Date.AddDays(1);
                    query = query.Where(x => x.CreatedAt < endExclusive);
                }
                else
                {
                    query = query.Where(x => x.CreatedAt <= to.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();

                query = query.Where(x =>
                    x.Reference.Contains(s) ||
                    (x.GatewayReference != null && x.GatewayReference.Contains(s)) ||
                    (x.InvoiceId != null && x.InvoiceId.Contains(s)));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto())
                .ToListAsync(ct);

            return new PagedResult<PaymentTransactionDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // Centralized projection (BEST PRACTICE)
        private static Expression<Func<PaymentTransaction, PaymentTransactionDto>> MapToDto()
        {
            return x => new PaymentTransactionDto
            {
                Id = x.Id,
                Reference = x.Reference,
                GatewayReference = x.GatewayReference,
                InvoiceId = x.InvoiceId,
                Amount = x.Amount,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            };
        }
    }

}
