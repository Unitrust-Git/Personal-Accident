using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.Persistence.Interface
{
    public interface IIndividualPolicyRepository
    {
        Task<IndividualPolicy?> GetById(Guid Id);
        Task AddAsync(IndividualPolicy policy);
        Task<IndividualPolicy?> GetByPolicyNumberAsync(string policyNumber);

        IQueryable<IndividualPolicy> Query();
        Task<List<IndividualPolicy>> GetAllAsync();

        Task<IndividualPolicy?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId);
        Task<bool> ExistsForPaymentAsync(Guid paymentTransactionId);
    }
}
