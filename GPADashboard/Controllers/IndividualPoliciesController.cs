using GPA.Application.Implementation;
using GPA.Application.Interface;
using GPA.Core.Enums;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.HttpClients;
using GPADashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPADashboard.Controllers
{
    [Authorize]
    public class IndividualPoliciesController : Controller
    {
        private readonly IIndividualPolicyService _service;
        private readonly IPolicyCertificateService _certificateService;
        private readonly IUniwareClient _uniwareClient;
        private readonly IPolicyCertificateService _policyCertificateService;

        public IndividualPoliciesController(
            IIndividualPolicyService service,
            IPolicyCertificateService certificateService,
            IUniwareClient uniwareClient,
            IPolicyCertificateService policyCertificateService)
        {
            _service = service;
            _certificateService = certificateService;
            _uniwareClient = uniwareClient;
            _policyCertificateService = policyCertificateService;   
        }

        [HttpGet]
        public async Task<IActionResult> Index( string? search, string? policyNumber,
        Guid? paymentTransactionId, int page = 1,  int pageSize = 20)
        {

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var (items, total) = await _service.GetPagedAsync(
                search,
                policyNumber,
                paymentTransactionId,
                page,
                pageSize);

            var vm = new IndividualPoliciesIndexVm
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Search = search,
                PolicyNumber = policyNumber,
                PaymentTransactionId = paymentTransactionId

            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var policy = await _service.GetById(id);

            if (policy == null) return NotFound();
            return Ok(policy);
        }

        // GET: /IndividualPolicies/ByPolicyNumber?policyNumber=...
        [HttpGet]
        public async Task<IActionResult> ByPolicyNumber(string policyNumber)
        {
            if (string.IsNullOrWhiteSpace(policyNumber))
                return BadRequest("policyNumber is required.");

            var policy = await _service.GetByPolicyNumberAsync(policyNumber);
            if (policy == null) return NotFound();

            return View("Details", policy);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken ct)
        {


            var policy = await _service.GetById(id);
            if (policy == null) return NotFound();

            if (string.IsNullOrWhiteSpace(policy.PolicyNumber))
                return BadRequest("Certificate not available until Policy Number is generated.");

            var pdfBytes = await _certificateService.GenerateIndividualPolicyCertificateAsync(policy, ct);


            var safePolicyNo = policy.PolicyNumber.Replace("/", "-").Replace("\\", "-");
            var fileName = $"Certificate-{safePolicyNo}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }


        [HttpGet]
        public async Task<IActionResult> ByPaymentTransaction(Guid paymentTransactionId)
        {
            if (paymentTransactionId == Guid.Empty)
                return BadRequest("paymentTransactionId is required.");

            var policy = await _service.GetByPaymentTransactionIdAsync(paymentTransactionId);
            if (policy == null) return NotFound();

            return View("Details", policy);
        }


        private async Task<List<PolicyReconciliationResult>> ReconcilePolicies(
            List<IndividualPolicy> policiesFromDb,
            CancellationToken ct = default)
        {
            var results = new List<PolicyReconciliationResult>();

            foreach (var policy in policiesFromDb)
            {
                if (string.IsNullOrWhiteSpace(policy.PolicyNumber))
                {
                    results.Add(new PolicyReconciliationResult
                    {
                        PolicyNumber = policy.PolicyNumber,
                        Status = "ERROR",
                        Remark = "Missing policy number"
                    });

                    continue;
                }

                var apiResponse = await _uniwareClient.GetPolicyDetailsAsync(policy.PolicyNumber, ct);
                var apiPolicy = apiResponse?.result?.FirstOrDefault();

                if (apiPolicy == null)
                {
                    results.Add(new PolicyReconciliationResult
                    {
                        PolicyNumber = policy.PolicyNumber,
                        Units = policy.Units,
                        ExpectedSumAssured = policy.Units * 1_000_000m,
                        ActualSumAssured = apiPolicy?.sum_assured,
                        Status = "NOT_FOUND_IN_UNIWARE",
                        Remark = "Policy not found in Uniware"
                    });

                    continue;
                }

                var expectedSumAssured = policy.Units * 1_000_000m;
                var actualSumAssured = apiPolicy.sum_assured ?? 0m;

                var isValid = Math.Abs(expectedSumAssured - actualSumAssured) < 0.01m;

                results.Add(new PolicyReconciliationResult
                {
                    PolicyNumber = policy.PolicyNumber,
                    Units = policy.Units,
                    ExpectedSumAssured = expectedSumAssured,
                    ActualSumAssured = actualSumAssured,
                    InsuredName = apiPolicy.insured,
                    ClientCode = apiPolicy.clientcode,
                    EffectiveDate = DateTime.TryParse(apiPolicy.effective_date, out var effDate) ? effDate : null,
                    EndDate = DateTime.TryParse(apiPolicy.end_date, out var endDate) ? endDate : null,
                    Status = isValid ? "VALID" : "MISMATCH",
                    Remark = isValid ? "OK" : "Sum assured mismatch"
                });
            }

            return results;
        }



        [HttpGet]
        public async Task<IActionResult> Reconciliation(
            int page = 1,
            int pageSize = 50)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 200);

            var (items, total) = await _service.GetPagedAsync(
                null, null, null,
                page,
                pageSize);

            var reconciliation = await ReconcilePolicies(
                items.ToList(),
                CancellationToken.None);

            var vm = new IndividualPoliciesIndexVm
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Reconciliation = reconciliation
            };

            ViewData["ActivePage"] = "Policy Reconciliation";

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPolicyEmail(Guid id,CancellationToken ct)
        {
            await _policyCertificateService.SendPolicyEmail(id, ct);

            return Ok();
        }
    }
}
