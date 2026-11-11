using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Implementation
{
    public class CorporatePolicyRepository : ICorporatePolicyRepository
    {
        private readonly ApplicationDbContext _context;

        public CorporatePolicyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(CorporatePolicy policy)
        {
            await _context.CorporatePolicies.AddAsync(policy);
        }

        public async Task<CorporatePolicy?> GetByPolicyNumberAsync(string policyNumber)
        {
            return await _context.CorporatePolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber);
        }

        public async Task<List<CorporatePolicy>> GetAllAsync()
        {
            return await _context.CorporatePolicies
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
