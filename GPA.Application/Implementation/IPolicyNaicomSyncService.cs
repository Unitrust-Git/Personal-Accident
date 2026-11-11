using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Implementation
{
    public interface IPolicyNaicomSyncService
    {
        Task SyncMissingNaicomIdsAsync(CancellationToken cancellationToken = default);
    }
}
