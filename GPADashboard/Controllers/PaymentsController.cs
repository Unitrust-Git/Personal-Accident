using GPA.Application.DTO;
using GPA.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace GPADashboard.Controllers
{
    using GPA.Application.DTO;
    using GPA.Application.Implementation.DasboardServices;
    using GPA.Core.Enums;
    using GPADashboard.Models;
    using Microsoft.AspNetCore.Mvc;

    public class PaymentsController : Controller
    {
        private readonly IPaymentTransactionReadService _readService;

        public PaymentsController(IPaymentTransactionReadService readService)
        {
            _readService = readService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            PaymentStatus? status,
            DateTime? from,
            DateTime? to,
            string? search,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _readService.GetPagedAsync(
                status, from, to, search,
                page, pageSize, ct);

            var vm = new PaymentsIndexVm
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                Status = status,
                From = from,
                To = to,
                Search = search
            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> DetailsModal(Guid id, CancellationToken ct = default)
        {
            var payment = await _readService.GetByIdAsync(id, ct);
            if (payment == null) return NotFound();

            return PartialView("_PaymentDetailsModalBody", new PaymentDetailsVm { Payment = payment });
        }


        [HttpGet]
        public async Task<IActionResult> ByReference(string reference, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return RedirectToAction(nameof(Index));

            var payment = await _readService.GetByReferenceAsync(reference.Trim(), ct);
            if (payment == null)
            {
                return RedirectToAction(nameof(Index), new { search = reference.Trim() });

            }

            return RedirectToAction(nameof(DetailsModal), new { id = payment.Id });
        }
    }

}
