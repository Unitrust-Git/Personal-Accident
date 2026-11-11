using GPA.Application.DTO;
using GPA.Application.Interface;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;

namespace GPA.Application.Implementation
{
    public class IndividualPolicyService : IIndividualPolicyService
    {
        private readonly IEmailSender _emailSender;
        private readonly IIndividualPolicyRepository _repo;
        //private readonly IPolicyCertificateService _certService;
        private readonly IUniwareClient _uniwareClient;
        private readonly ApplicationDbContext _context;

        public IndividualPolicyService(
            IIndividualPolicyRepository repo,
            IUniwareClient uniwareClient,
            IEmailSender emailSender,
            //IPolicyCertificateService certService,
            ApplicationDbContext context)
        {
            _repo = repo;
            _uniwareClient = uniwareClient;
            _context = context;
            _emailSender = emailSender;
            //_certService = certService;
        }

        public async Task CreateAsync(IndividualPolicy policy)
        {
            await _repo.AddAsync(policy);
            await _context.SaveChangesAsync();
        }

        public async Task<IndividualPolicy?> GetById(Guid Id)
        {
            return await _repo.GetById(Id);
        }

        public async Task<IndividualPolicy?> GetByPolicyNumberAsync(string policyNumber)
        {
            return await _repo.GetByPolicyNumberAsync(policyNumber);
        }

        public async Task<List<IndividualPolicy>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<(List<IndividualPolicy> Items, int Total)> GetPagedAsync(
        string? search, string? policyNumber, Guid? paymentTransactionId,
        int page, int pageSize)
        {
            var query = _repo.Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(policyNumber))
                query = query.Where(x => x.PolicyNumber == policyNumber);

            if (paymentTransactionId.HasValue)
                query = query.Where(x => x.PaymentTransactionId == paymentTransactionId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x =>
                    (x.PolicyNumber ?? "").Contains(s) ||
                    (x.Email ?? "").Contains(s) ||
                    (x.FirstName ?? "").Contains(s) ||
                    (x.LastName ?? "").Contains(s) ||
                    (x.NIN ?? "").Contains(s));
            }

            query = query.OrderByDescending(x => x.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }

        public async Task<IndividualPolicy?> GetByPaymentTransactionIdAsync(Guid paymentTransactionId)
        {
            return await _repo.GetByPaymentTransactionIdAsync(paymentTransactionId);
        }

        public async Task<bool> ExistsForPaymentAsync(Guid paymentTransactionId)
        {
            return await _repo.ExistsForPaymentAsync(paymentTransactionId);
        }


        public async Task<List<PolicyReconciliationResult>> ReconcileAsync(
            List<IndividualPolicy> policies,
            CancellationToken ct = default)
        {
            var results = new List<PolicyReconciliationResult>();

            foreach (var policy in policies)
            {
                if (string.IsNullOrWhiteSpace(policy.PolicyNumber))
                    continue;

                var apiResponse = await _uniwareClient.GetPolicyDetailsAsync(policy.PolicyNumber, ct);
                var apiPolicy = apiResponse?.result?.FirstOrDefault();

                if (apiPolicy == null)
                {
                    results.Add(new PolicyReconciliationResult
                    {
                        PolicyNumber = policy.PolicyNumber,
                        Status = "NOT_FOUND_IN_UNIWARE"
                    });

                    continue;
                }

                var expected = policy.Units * 1_000_000m;
                var actual = apiPolicy.sum_assured ?? 0;

                var isValid = Math.Abs(expected - actual) < 0.01m;

                results.Add(new PolicyReconciliationResult
                {
                    PolicyNumber = policy.PolicyNumber,
                    Units = policy.Units,
                    ExpectedSumAssured = expected,
                    ActualSumAssured = actual,
                    Status = isValid ? "VALID" : "MISMATCH"
                });
            }

            return results;
        }




    }
}
