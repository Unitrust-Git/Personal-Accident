using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Implementation
{
    public interface IPolicyProvisioningAdminService
    {
        Task ResetProvisioningAsync(Guid jobId, string scope, CancellationToken ct);
        Task RetryFromFirstIncompleteStepAsync(Guid jobId, CancellationToken ct);
    }

}
