using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Interface
{
    public interface ICorporatePolicyRepository
    {
        Task AddAsync(CorporatePolicy policy);
        Task<CorporatePolicy?> GetByPolicyNumberAsync(string policyNumber);

        Task<List<CorporatePolicy>> GetAllAsync();
    }
}
