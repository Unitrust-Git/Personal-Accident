using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Interface
{
    public interface ICorporatePolicyBeneficiaryRepository
    {
        Task AddRangeAsync(IEnumerable<CorporatePolicyBeneficiary> beneficiaries);
    }
}
