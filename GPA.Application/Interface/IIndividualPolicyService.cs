using GPA.Application.DTO;
using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface IIndividualPolicyService
    {
        Task CreateAsync(IndividualPolicy policy);

        Task<IndividualPolicy?> GetByPolicyNumberAsync(string policyNumber);

        Task<IndividualPolicy?> GetById(Guid Id);

        Task<List<IndividualPolicy>> GetAllAsync();


        Task<(List<IndividualPolicy> Items, int Total)> GetPagedAsync(
        string? search, string? policyNumber, Guid? paymentTransactionId,
        int page, int pageSize);

        Task<IndividualPolicy?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId);

        Task<bool> ExistsForPaymentAsync(Guid paymentTransactionId);

        Task<List<PolicyReconciliationResult>> ReconcileAsync(
            List<IndividualPolicy> policies,
            CancellationToken ct = default);

    }
}
