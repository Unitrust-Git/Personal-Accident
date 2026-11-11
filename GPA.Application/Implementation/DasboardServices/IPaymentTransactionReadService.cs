using GPA.Application.Common;
using GPA.Application.DTO;
using GPA.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Implementation.DasboardServices
{
    public interface IPaymentTransactionReadService
    {
        Task<PaymentTransactionDto?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<PaymentTransactionDto?> GetByReferenceAsync(string reference, CancellationToken ct);

        Task<PagedResult<PaymentTransactionDto>> GetPagedAsync(
            PaymentStatus? status,
            DateTime? from,
            DateTime? to,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct);
    }

}
