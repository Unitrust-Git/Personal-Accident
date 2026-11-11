using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Interface
{
    public interface IPolicyRepository
    {
        Task<PolicyBase?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId);
        Task<IndividualPolicy?> GetIndividualTrackedAsync(Guid paymentTransactionId, CancellationToken ct = default);
        Task<CorporatePolicy?> GetCorporateTrackedAsync(Guid paymentTransactionId, CancellationToken ct = default);

        Task<bool> ExistsForPaymentAsync(Guid paymentTransactionId, CancellationToken ct = default);

        Task AddIndividualAsync(IndividualPolicy policy, CancellationToken ct = default);
        Task AddCorporateAsync(CorporatePolicy policy, CancellationToken ct = default);

        Task<bool> TryAcquireLeaseAsync(Guid policyId, string lockedBy, Guid lockToken, TimeSpan leaseDuration, CancellationToken ct = default);
        Task ReleaseLeaseAsync(Guid policyId, Guid lockToken, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
        Task SaveAsync(PolicyBase policy, CancellationToken ct = default);

    }

}
