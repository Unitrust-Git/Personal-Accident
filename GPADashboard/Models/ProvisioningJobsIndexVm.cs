using GPA.Application.Common;
using GPA.Application.DTO;
using GPA.Core.Models.Policy;

namespace GPADashboard.Models
{


    public class ProvisioningJobsIndexVm
    {
        public PagedResult<PolicyProvisioningJobDto> Result { get; set; } = default!;

        public ProvisioningStatus? Status { get; set; }
        public string? PaymentType { get; set; }
        public DateTime? DueBefore { get; set; }
        public Guid? PaymentTransactionId { get; set; }
        public string? Search { get; set; }
    }

}
