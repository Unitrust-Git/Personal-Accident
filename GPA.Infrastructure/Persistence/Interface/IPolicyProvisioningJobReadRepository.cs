using GPA.Core.Models.Policy;

namespace GPA.Infrastructure.Persistence.Interface
{
    using GPA.Core.Models.Policy;

    public interface IPolicyProvisioningJobReadRepository
    {
        Task<PolicyProvisioningJob?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<(IReadOnlyList<PolicyProvisioningJob> Items, int TotalCount)> GetPagedAsync(
            ProvisioningStatus? status,
            string? paymentType,
            DateTime? dueBefore,
            Guid? paymentTransactionId,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct);

        Task<PolicyProvisioningJob?> GetByPaymentTransactionAsync(
            Guid paymentTransactionId,
            string paymentType,
            CancellationToken ct);

        Task<ProvisioningStatus?> GetStatusAsync(
            Guid paymentTransactionId,
            string paymentType,
            CancellationToken ct);


    }

}
