using GPA.Core.Models.Policy;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;

namespace GPA.Infrastructure.Persistence.Implementation
{
    public class IndividualPolicyRepository : IIndividualPolicyRepository
    {
        private readonly ApplicationDbContext _context;

        public IndividualPolicyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(IndividualPolicy policy)
        {
            await _context.IndividualPolicies.AddAsync(policy);
        }

        public async Task<IndividualPolicy?> GetByPolicyNumberAsync(string policyNumber)
        {
            return await _context.IndividualPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber);
        }

        public async Task<IndividualPolicy?> GetById(Guid Id)
        {
            return await _context.IndividualPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == Id);
        }

        public IQueryable<IndividualPolicy> Query()
        {
            return _context.IndividualPolicies.AsNoTracking();
        }

        public async Task<List<IndividualPolicy>> GetAllAsync()
        {
            return await _context.IndividualPolicies
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IndividualPolicy?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId)
        {
            return await _context.IndividualPolicies
                .AsNoTracking()
                .Include(p => p.PaymentTransaction) 
                .FirstOrDefaultAsync(p => p.PaymentTransactionId == paymentTransactionId);
        }
        public async Task<bool> ExistsForPaymentAsync(Guid paymentTransactionId)
        {
            return await _context.IndividualPolicies
                .AnyAsync(p => p.PaymentTransactionId == paymentTransactionId);
        }
    }
}
