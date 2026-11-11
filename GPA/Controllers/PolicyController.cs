using Azure.Core;
using GPA.Application.DTO;
using GPA.Application.Implementation;
using GPA.Application.Interface;
using GPA.Core.Enums;
using GPA.Core.Models.Policy;
using GPA.Helpers;
using GPA.Infrastructure.Persistence.Interface;
using GPA.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

public class PolicyController : Controller
{
    private readonly IIndividualPolicyService _individualPolicyService;
    private readonly ICorporatePolicyService _corporatePolicyService;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly IPolicyCertificateService _certificateService;
    private readonly IPolicyProvisioningJobReadService _jobReadService;



    public PolicyController(
        IIndividualPolicyService individualPolicyService,
        ICorporatePolicyService corporatePolicyService,
        IPaymentTransactionRepository paymentTransactionRepository,
        IPolicyProvisioningJobReadService jobReadService,
        IPolicyCertificateService certificateService)
    {
        _individualPolicyService = individualPolicyService;
        _corporatePolicyService = corporatePolicyService;
        _paymentTransactionRepository = paymentTransactionRepository;
        _certificateService = certificateService;
        _jobReadService = jobReadService;

    }

    public async Task<IActionResult> InvoiceCallback(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return Redirect("/policy/failed?reason=missing-reference");

        var ct = HttpContext.RequestAborted;
        var encodedRef = Uri.EscapeDataString(reference);

        var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, ct);
        if (tx == null)
            return Redirect($"/policy/error?reason=not-found&reference={Uri.EscapeDataString(encodedRef)}");

        if (tx.Status == PaymentStatus.Failed)
            return Redirect($"/policy/error?reason=paymentfailed&reference={Uri.EscapeDataString(encodedRef)}");

        if (tx.Status != PaymentStatus.Success)
            return Redirect($"/policy/pending?reference={Uri.EscapeDataString(encodedRef)}");

        PaymentMetadata? metadata;
        try
        {
            metadata = JsonSerializer.Deserialize<PaymentMetadata>(
                tx.Metadata ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return Redirect($"/policy/error?reason=invalid-metadata&reference={Uri.EscapeDataString(encodedRef)}");
        }

        var paymentType = metadata?.PaymentType?.ToLowerInvariant() ?? "individual";

        var job = await _jobReadService
            .GetByPaymentTransactionAsync(tx.Id, paymentType, ct);

        if (job == null ||
            job.Status == ProvisioningStatus.Pending ||
            job.Status == ProvisioningStatus.Processing)
        {
            ViewBag.Reference = reference;
            ViewBag.Message = "Your policy is being created. Please wait up to 5 minutes.";
            ViewBag.SubMessage = "Your certificate will be sent to your email once ready.";

            return View("InvoiceWaiting");
        }

        if (job.Status == ProvisioningStatus.Succeeded)
        {
            var policy = paymentType switch
            {
                "individual" => await _individualPolicyService
                    .GetByPaymentTransactionIdAsync(tx.Id),

                _ => null
            };

            if (policy == null)
            {
                ViewBag.Reference = reference;
                return View("InvoiceWaiting");
            }

            var vm = new CertificateViewVm
            {
                Reference = reference,
                PolicyNumber = policy.PolicyNumber,
                PaymentType = paymentType
            };

            return View("Certificate", vm);
        }

        if (job.Status == ProvisioningStatus.Failed ||
            job.Status == ProvisioningStatus.PermanentFailed)
        {
            var proofVm = new PaymentProofViewVm
            {
                Reference = reference,
                Amount = tx.Amount,
                PaymentDate = tx.UpdatedAt,
                PaymentType = paymentType,
                TransactionReference = tx.Reference,
                FailureReason = job.LastError
            };

            return View("PaymentProofCertificate", proofVm);
        }

        return Redirect($"/policy/error?reason=unknown&reference={encodedRef}");


    }

    [HttpGet("/policy/pending")]
    public IActionResult Pending([FromQuery] string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return Redirect("/policy/failed?reason=missing-reference");


        var checkStatusUrl = Url.Action(
            action: "Status",
            controller: "Policy",
            values: new { reference },
            protocol: Request.Scheme
        );


        ViewBag.Reference = reference;
        ViewBag.CheckStatusUrl = checkStatusUrl;

        return View("Pending");
    }

    [HttpGet("/policy/failed")]
    public async Task<IActionResult> Failed([FromQuery] string reason, [FromQuery] string? reference)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim().ToLowerInvariant();

        ViewBag.Reason = reason;
        ViewBag.Reference = reference;

        if (!string.IsNullOrWhiteSpace(reference))
        {
            var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, HttpContext.RequestAborted);
            ViewBag.TransactionStatus = tx?.Status.ToString();
            ViewBag.GatewayResponse = tx?.FailureReason;
            ViewBag.Amount = tx?.Amount;
        }

        return View("Failed");
    }

    [HttpGet("/policy/status")]
    public async Task<IActionResult> Status(string reference)
    {

        var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, HttpContext.RequestAborted);

        if (tx == null)
            return Json(new { state = "failed" });

        if (tx.Status == PaymentStatus.Failed)
            return Json(new { state = "failed" });

        if (tx.Status != PaymentStatus.Success)
            return Json(new { state = "pending" });

        return Json(new
        {
            state = "ready",
            redirectUrl = Url.Action("InvoiceCallback", "Policy", new { reference })
        });
    }

    [HttpGet("/policy/error")]
    public async Task<IActionResult> Error([FromQuery] string reason, [FromQuery] string? reference)
    {
        reason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim().ToLowerInvariant();

        ViewBag.Reason = reason;
        ViewBag.Reference = reference;

        if (!string.IsNullOrWhiteSpace(reference))
        {
            var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, HttpContext.RequestAborted);

            ViewBag.Amount = tx?.Amount;
            ViewBag.TransactionStatus = tx?.Status.ToString();
        }

        return View("Error");
    }



    //[HttpGet]
    //public async Task<IActionResult> CertificatePdf(string reference)
    //{
    //    if (string.IsNullOrWhiteSpace(reference))
    //        return BadRequest("missing reference");

    //    var ct = HttpContext.RequestAborted;

    //    var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, ct);
    //    if (tx == null) return NotFound();

    //    if (tx.Status != PaymentStatus.Success) return BadRequest("payment not successful");

    //    var policy = await _individualPolicyService.GetByPaymentTransactionIdAsync(tx.Id);
    //    if (policy == null) return NotFound();

    //    var pdfBytes = await _certificateService.GenerateIndividualPolicyCertificateAsync(policy, ct);

    //    Response.Headers["Content-Disposition"] = $"inline; filename=Certificate-{policy.PolicyNumber}.pdf";

    //    return File(pdfBytes, "application/pdf");
    //}

    [HttpGet]
    public async Task<IActionResult> CertificatePdf(string reference, bool download = false)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return BadRequest("missing reference");

        var ct = HttpContext.RequestAborted;

        var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, ct);
        if (tx == null) return NotFound();

        if (tx.Status != PaymentStatus.Success) return BadRequest("payment not successful");

        var policy = await _individualPolicyService.GetByPaymentTransactionIdAsync(tx.Id);
        if (policy == null) return NotFound();

        var pdfBytes = await _certificateService.GenerateIndividualPolicyCertificateAsync(policy, ct);

        var fileName = $"Certificate-{policy.PolicyNumber}.pdf";

        // Optional but recommended:
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (download)
            return File(pdfBytes, "application/pdf", fileName); // attachment + filename
        else
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";

        return File(pdfBytes, "application/pdf");
    }
    
    //[HttpGet("/policy/proof-pdf")]
    //public async Task<IActionResult> ProofPdf(string reference)
    //{
    //    if (string.IsNullOrWhiteSpace(reference))
    //        return BadRequest("missing reference");

    //    var ct = HttpContext.RequestAborted;

    //    var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, ct);
    //    if (tx == null) return NotFound();

    //    if (tx.Status != PaymentStatus.Success)
    //        return BadRequest("payment not successful");

    //    PaymentMetadata? metadata;
    //    try
    //    {
    //        metadata = JsonSerializer.Deserialize<PaymentMetadata>(
    //            tx.Metadata ?? "{}",
    //            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    //    }
    //    catch
    //    {
    //        return BadRequest("invalid metadata");
    //    }

    //    var paymentType = metadata?.PaymentType?.ToLowerInvariant() ?? "individual";

    //    var job = await _jobReadService.GetByPaymentTransactionAsync(tx.Id, paymentType, ct);
    //    if (job == null) return BadRequest("provisioning not started");

    //    if (job.Status != ProvisioningStatus.Failed && job.Status != ProvisioningStatus.PermanentFailed)
    //        return BadRequest("proof not available");

    //    var vm = new PaymentProofData
    //    {
    //        Reference = reference,
    //        Amount = tx.Amount,
    //        PaymentDate = tx.UpdatedAt,
    //        TransactionReference = tx.Reference,
    //    };

    //    var pdfBytes = await _certificateService.GeneratePaymentProofCertificateAsync(vm, ct);

    //    Response.Headers["Content-Disposition"] =
    //        $"attachment; filename=Payment-Proof-{reference}.pdf";

    //    return File(pdfBytes, "application/pdf");
    //}

    [HttpGet("/policy/proof-pdf")]
    public async Task<IActionResult> ProofPdf(string reference, bool download = false)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return BadRequest("missing reference");

        var ct = HttpContext.RequestAborted;

        var tx = await _paymentTransactionRepository.GetByReferenceAsync(reference, ct);
        if (tx == null) return NotFound();

        if (tx.Status != PaymentStatus.Success)
            return BadRequest("payment not successful");

        PaymentMetadata? metadata;
        try
        {
            metadata = JsonSerializer.Deserialize<PaymentMetadata>(
                tx.Metadata ?? "{}",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return BadRequest("invalid metadata");
        }

        var paymentType = metadata?.PaymentType?.ToLowerInvariant() ?? "individual";

        var job = await _jobReadService.GetByPaymentTransactionAsync(tx.Id, paymentType, ct);
        if (job == null) return BadRequest("provisioning not started");

        if (job.Status != ProvisioningStatus.Failed && job.Status != ProvisioningStatus.PermanentFailed)
            return BadRequest("proof not available");

        var vm = new PaymentProofData
        {
            Reference = reference,
            Amount = tx.Amount,
            PaymentDate = tx.UpdatedAt,
            TransactionReference = tx.Reference,
            metadata = metadata
        };

        var pdfBytes = await _certificateService.GeneratePaymentProofCertificateAsync(vm, ct);

        var fileName = $"Payment-Proof-{reference}.pdf";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (download)
            return File(pdfBytes, "application/pdf", fileName);

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");

    }

}
