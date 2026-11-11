    using GPA.Application.Implementation;
    using GPA.Application.Interface;
    using GPA.Core.Models.Policy;
    using GPADashboard.Models;
    using Microsoft.AspNetCore.Mvc;

    namespace GPADashboard.Controllers
    {
        public class PolicyQueueController : Controller
        {
            private readonly IPolicyProvisioningJobReadService _read;
            private readonly IPolicyProvisioningAdminService _admin;

            public PolicyQueueController(
                IPolicyProvisioningJobReadService read,
                IPolicyProvisioningAdminService admin)
            {
                _read = read;
                _admin = admin;
            }

            [HttpGet]
            public async Task<IActionResult> Index(
                ProvisioningStatus? status,
                string? paymentType,
                DateTime? dueBefore,
                Guid? paymentTransactionId,
                string? search,
                int page = 1,
                int pageSize = 20,
                CancellationToken ct = default)
            {
                var result = await _read.GetPagedAsync(
                    status, paymentType, dueBefore, paymentTransactionId, search, page, pageSize, ct);

                var vm = new ProvisioningJobsIndexVm
                {
                    Result = result,
                    Status = status,
                    PaymentType = paymentType,
                    DueBefore = dueBefore,
                    PaymentTransactionId = paymentTransactionId,
                    Search = search
                };

                return View(vm);
            }

            [HttpGet]
            public async Task<IActionResult> DetailsModal(Guid id, CancellationToken ct = default)
            {
                var job = await _read.GetByIdAsync(id, ct);
                if (job == null) return NotFound();

                return PartialView("_ProvisioningJobDetailsModalBody", job);
            }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Reset(Guid id, string scope, CancellationToken ct = default)
            {

                await _admin.ResetProvisioningAsync(id, scope, ct);

                return RedirectToAction(nameof(Index));
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Retry(Guid id, CancellationToken ct = default)
            {
                try
                {
                    await _admin.RetryFromFirstIncompleteStepAsync(id, ct);
                    TempData["Success"] = "Retry queued. Worker will resume from the first incomplete step.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                }

                return RedirectToAction(nameof(Index));
            }


    }
}
