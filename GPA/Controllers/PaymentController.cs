using GPA.Application.DTO;
using GPA.Application.Interface;
using GPA.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace GPA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly string _secret;

        public PaymentController(IPaymentService paymentService, IConfiguration config)
        {
            _paymentService = paymentService;
            _secret = config["Paystack:SecretKey"];
        }

        //[HttpPost("initialize")]
        //public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentRequest request, CancellationToken ct)
        //{
        //    var invoiceId = HttpContext.Session.GetString("currentInvoiceId");
        //    var result = await _paymentService.InitializeAsync(request, invoiceId, ct);
        //    return Ok(result);
        //}


        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentRequest request,
            CancellationToken ct)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid payment request."
                    });
                }


                var invoiceId = HttpContext.Session
                    .GetString("currentInvoiceId");


                if (string.IsNullOrEmpty(invoiceId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invoice session has expired. Please refresh."
                    });
                }


                var result = await _paymentService
                    .InitializeAsync(request, invoiceId, ct);


                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new
                {
                    success = false,
                    message = "Payment initialization was cancelled."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to initialize payment. Please try again later."
                });
            }
        }


        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();



            if (!Request.Headers.TryGetValue("x-paystack-signature", out var signature))
                return Unauthorized("Missing signature");

            if (!IsValidPaystackSignature(body, _secret, signature))
                return Unauthorized("Invalid signature");


            PaystackWebhookPayload? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<PaystackWebhookPayload>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON payload: {ex.Message}");
            }

            if (webhook == null || string.IsNullOrWhiteSpace(webhook.Event))
                return BadRequest("Webhook payload missing 'event' field.");

            PaymentProcessingResult result;

            try
            {
                switch (webhook.Event.ToLowerInvariant())
                {
                    case "charge.success":
                        result = await _paymentService.ProcessPaymentSuccessAsync(webhook.Data);
                        break;

                    case "charge.failed":
                        result = await _paymentService.ProcessPaymentFailureAsync(webhook.Data);
                        break;

                    default:
                        return NoContent();
                }
            }
            catch (Exception ex)
            {

                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Error processing webhook: {ex.Message}",
                    Exception = ex.Message
                });
            }

            return Ok(new
            {
                Success = result.Success,
                Message = result.Message,
                TransactionReference = result.TransactionReference
            });
        }

        private static bool IsValidPaystackSignature(string body, string secret, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signatureHeader))
                return false;

            var signature = signatureHeader.Trim().ToLowerInvariant();

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHex),
                Encoding.UTF8.GetBytes(signature)
            );
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string reference, [FromQuery] string? trxref)
        {
            var refToUse = !string.IsNullOrWhiteSpace(reference) ? reference : trxref;

            if (string.IsNullOrWhiteSpace(refToUse))
                return Redirect("/payment/failed?reason=missing-reference");

            var redirectUrl = await _paymentService.HandlePaymentCallbackAsync(refToUse ?? "");
            return Redirect(redirectUrl);
        }
    }
}
