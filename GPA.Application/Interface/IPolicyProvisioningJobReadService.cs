using GPA.Application.Common;
using GPA.Application.DTO;
using GPA.Core.Models.Policy;

namespace GPA.Application.Interface
{


    public interface IPolicyProvisioningJobReadService
    {
        Task<PolicyProvisioningJobDto?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<PagedResult<PolicyProvisioningJobDto>> GetPagedAsync(
            ProvisioningStatus? status,
            string? paymentType,
            DateTime? dueBefore,
            Guid? paymentTransactionId,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct);

        Task<PolicyProvisioningJobDto?> GetByPaymentTransactionAsync(
            Guid paymentTransactionId,
            string paymentType,
            CancellationToken ct);

    }

}
