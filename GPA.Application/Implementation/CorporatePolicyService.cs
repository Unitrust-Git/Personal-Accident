using GPA.Application.Interface;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Interface;

namespace GPA.Application.Implementation
{
    public class CorporatePolicyService : ICorporatePolicyService
    {
        private readonly ICorporatePolicyRepository _repo;
        private readonly ApplicationDbContext _context;

        public CorporatePolicyService(
            ICorporatePolicyRepository repo,
            ApplicationDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task CreateAsync(CorporatePolicy policy)
        {
            await _repo.AddAsync(policy);
            await _context.SaveChangesAsync();
        }

        public async Task<CorporatePolicy?> GetByPolicyNumberAsync(string policyNumber)
        {
            return await _repo.GetByPolicyNumberAsync(policyNumber);
        }

        public async Task<List<CorporatePolicy>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }
    }
}
