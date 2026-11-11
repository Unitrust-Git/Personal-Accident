using GPA.Core.Enums;
using GPA.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Interface
{
    public interface IPaymentTransactionRepository
    {
        Task<(IReadOnlyList<PaymentTransaction> Items, int TotalCount)> GetPagedAsync(
            PaymentStatus? status,
            DateTime? from,
            DateTime? to,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct);

        Task AddAsync(PaymentTransaction tx, CancellationToken ct);
        Task<PaymentTransaction?> GetByReferenceAsync(string reference, CancellationToken ct);
        Task UpdateAsync(PaymentTransaction tx, CancellationToken ct);

        Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct);
    }

}
