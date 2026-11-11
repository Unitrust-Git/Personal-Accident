using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Implementation
{
    public class CorporatePolicyBeneficiaryRepository
        : ICorporatePolicyBeneficiaryRepository
    {
        private readonly ApplicationDbContext _context;

        public CorporatePolicyBeneficiaryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<CorporatePolicyBeneficiary> beneficiaries)
        {
            await _context.CorporatePolicyBeneficiaries.AddRangeAsync(beneficiaries);
        }
    }
}
