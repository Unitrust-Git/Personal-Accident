using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface ICorporatePolicyService
    {
        Task CreateAsync(CorporatePolicy policy);

        Task<CorporatePolicy?> GetByPolicyNumberAsync(string policyNumber);

        Task<List<CorporatePolicy>> GetAllAsync();
    }
}
