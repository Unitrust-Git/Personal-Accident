using GPA.Application.DTO;
using GPA.Application.Interface;
using GPA.Application.Utility;
using GPA.Core.Enums;
using GPA.Core.Models;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GPA.Application.Implementation
{
    public class PaymentService : IPaymentService
    {

        private readonly IPaymentTransactionRepository _repo;
        private readonly IPaystackClient _paystack;
        private readonly ApplicationDbContext _context;
        private readonly IPolicyCreationService _policyCreationService;
        private readonly IIndividualPolicyService _individualPolicyService;
        private readonly ICorporatePolicyService _corporatePolicyService;
        private readonly ILogger<PaymentService> _logger;


        public PaymentService(
            IPaymentTransactionRepository repo,
            IPaystackClient paystack,
            ApplicationDbContext context,
            IPolicyCreationService policyCreationService,
            IIndividualPolicyService individualPolicyService,
            ICorporatePolicyService corporatePolicyService,
            ILogger<PaymentService> logger)
        {
            _repo = repo;
            _paystack = paystack;
            _context = context;
            _policyCreationService = policyCreationService;
            _individualPolicyService = individualPolicyService;
            _corporatePolicyService = corporatePolicyService;
            _logger = logger;
        }


        public async Task<InitializePaymentResponse> InitializeAsync(
            InitializePaymentRequest request, string invoiceId,
            CancellationToken ct)
        {
            var strategy = _context.Database.CreateExecutionStrategy();


            return await strategy.ExecuteAsync(async () =>
            {
                var existingTx = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.InvoiceId == invoiceId, ct);

                if (existingTx != null)
                {
                    if (existingTx.Status == PaymentStatus.Success)
                        throw new InvalidOperationException("Payment already completed for this invoice.");

                    if (existingTx.Status == PaymentStatus.Pending)
                    {
                        if (UrlExpired(existingTx)) 
                        {
                            var newPaystack = await _paystack.Initialize(existingTx, ct);
                            existingTx.GatewayReference = newPaystack.Data.Reference;
                            existingTx.AccessCode = newPaystack.Data.AccessCode;
                            existingTx.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync(ct);
                            return new InitializePaymentResponse
                            {
                                AuthorizationUrl = newPaystack.Data.AuthorizationUrl,
                                Reference = existingTx.Reference
                            };
                        }

                        
                        return new InitializePaymentResponse
                        {
                            AuthorizationUrl = await _paystack.GetAuthorizationUrl(existingTx, ct),
                            Reference = existingTx.Reference
                        };
                    }
                }

                await using var dbTx = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    var tx = new PaymentTransaction
                    {
                        Id = Guid.NewGuid(),
                        Reference = ReferenceGenerator.Generate(),
                        InvoiceId = invoiceId,
                        AmountInKobo = (int)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero),
                        Amount = request.Amount,
                        Status = PaymentStatus.Pending,
                        Metadata = request.Metadata,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.PaymentTransactions.Add(tx);
                    await _context.SaveChangesAsync(ct);

                    var paystackResponse = await _paystack.Initialize(tx, ct);

                    tx.GatewayReference = paystackResponse.Data.Reference;
                    tx.AccessCode = paystackResponse.Data.AccessCode;
                    tx.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(ct);

                    await dbTx.CommitAsync(ct);

                    return new InitializePaymentResponse
                    {
                        AuthorizationUrl = paystackResponse.Data.AuthorizationUrl,
                        Reference = tx.Reference
                    };
                }
                catch
                {
                    await dbTx.RollbackAsync(ct);
                    throw;
                }
            });
        }

        private bool UrlExpired(PaymentTransaction tx)
        {
            return (DateTime.UtcNow - tx.UpdatedAt).TotalHours > 1;
        }



        public async Task<PaymentProcessingResult> ProcessPaymentSuccessAsync(PaystackPaymentData data, CancellationToken ct = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var tx = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t => t.Reference == data.Reference);

                    if (tx == null)
                    {
                        return new PaymentProcessingResult
                        {
                            Success = false,
                            Message = $"Payment transaction with reference '{data.Reference}' not found."
                        };
                    }

                    if (tx.Status == PaymentStatus.Success)
                    {
                        return new PaymentProcessingResult
                        {
                            Success = true,
                            Message = "Payment already processed.",
                            TransactionReference = tx.Reference
                        };
                    }

                    tx.Status = PaymentStatus.Success;
                    tx.UpdatedAt = DateTime.UtcNow;

                    if (data.Metadata != null)
                        tx.Metadata = JsonSerializer.Serialize(data.Metadata);

                    await _context.SaveChangesAsync();

                    var paymentType = data.Metadata?.PaymentType?.ToLowerInvariant();

                    await UpsertProvisioningJobAsync(tx.Id, paymentType,ct);

                    await transaction.CommitAsync();

                    return new PaymentProcessingResult
                    {
                        Success = true,
                        Message = "Payment processed",
                        TransactionReference = tx.Reference
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return new PaymentProcessingResult
                    {
                        Success = false,
                        Message = $"Error processing payment: {ex.Message}",
                        TransactionReference = data.Reference,
                        Exception = ex
                    };
                }
            });
        }





        public async Task<PaymentProcessingResult> ProcessPaymentFailureAsync(PaystackPaymentData data)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var tx = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t => t.Reference == data.Reference);

                    if (tx == null)
                        return new PaymentProcessingResult
                        {
                            Success = false,
                            Message = "Payment transaction not found",
                            TransactionReference = data.Reference
                        };

                    if (tx.Status == PaymentStatus.Failed)
                        return new PaymentProcessingResult
                        {
                            Success = false,
                            Message = "Payment transaction already marked as failed",
                            TransactionReference = data.Reference
                        };

                    tx.Status = PaymentStatus.Failed;
                    tx.FailureReason = data.Status ?? "Payment failed";
                    tx.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return new PaymentProcessingResult
                    {
                        Success = true,
                        Message = "Payment marked as failed",
                        TransactionReference = tx.Reference
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<string> HandlePaymentCallbackAsync(string reference, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                _logger.LogWarning("Payment callback missing reference");
                return "/policy/failed?reason=missing-reference";
            }

            var encodedRef = Uri.EscapeDataString(reference);

            var tx = await _repo.GetByReferenceAsync(reference, ct);
            if (tx == null)
            {
                _logger.LogWarning("Payment callback reference not found: {Reference}", reference);
                return $"/policy/failed?reason=not-found&reference={encodedRef}";
            }

            _logger.LogInformation("Payment callback resolved: Reference={Reference}, Status={Status}", reference, tx.Status);

            return tx.Status switch
            {
                PaymentStatus.Success => $"/policy/invoicecallback?reference={encodedRef}",
                PaymentStatus.Pending => $"/policy/pending?reference={encodedRef}",
                PaymentStatus.Failed => $"/policy/failed?reason=failed&reference={encodedRef}",
                _ => $"/policy/failed?reason=unknown&reference={encodedRef}"
            };
        }


        //public async Task<string> HandlePaymentCallbackAsync(string reference, CancellationToken ct = default)
        //{
        //    if (string.IsNullOrWhiteSpace(reference))
        //    {
        //        _logger.LogWarning("Payment callback missing reference");
        //        return "/policy/failed?reason=missing-reference";
        //    }

        //    var encodedRef = Uri.EscapeDataString(reference);

        //    var tx = await _repo.GetByReferenceAsync(reference, ct);
        //    if (tx == null)
        //    {
        //        _logger.LogWarning("Payment callback reference not found: {Reference}", reference);
        //        return $"/policy/failed?reason=not-found&reference={encodedRef}";
        //    }

        //    _logger.LogInformation(
        //        "Payment callback resolved: Reference={Reference}, Status={Status}",
        //        reference,
        //        tx.Status);

        //    if (tx.Status == PaymentStatus.Success)
        //    {

        //        var metadata = JsonSerializer.Deserialize<PaymentMetadata>(
        //            tx.Metadata ?? "{}",
        //            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //        var paymentType = metadata?.PaymentType?.ToLowerInvariant();

        //        bool policyExists = paymentType switch
        //        {
        //            "individual" => await _individualPolicyService.ExistsForPaymentAsync(tx.Id),

        //            _ => false
        //        };

        //        if (!policyExists)
        //            return $"/policy/pending?reference={encodedRef}";

        //        return $"/policy/invoicecallback?reference={encodedRef}";
        //    }

        //    return tx.Status switch
        //    {
        //        PaymentStatus.Pending => $"/policy/pending?reference={encodedRef}",
        //        PaymentStatus.Failed => $"/policy/failed?reason=failed&reference={encodedRef}",
        //        _ => $"/policy/failed?reason=unknown&reference={encodedRef}"
        //    };
        //}

        private async Task UpsertProvisioningJobAsync(Guid txId, string? paymentType, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(paymentType))
                throw new InvalidOperationException("Missing payment type.");

            var now = DateTime.UtcNow;

            var job = await _context.Set<PolicyProvisioningJob>()
                .SingleOrDefaultAsync(j => j.PaymentTransactionId == txId && j.PaymentType == paymentType, ct);

            if (job == null)
            {
                job = new PolicyProvisioningJob
                {
                    PaymentTransactionId = txId,
                    PaymentType = paymentType,
                    Status = ProvisioningStatus.Pending,
                    AttemptCount = 0,
                    NextAttemptAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Add(job);
                try
                {
                    await _context.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Another webhook thread created it first. That's fine.
                }
            }
            else
            {
                if (job.Status is ProvisioningStatus.Failed or ProvisioningStatus.Pending)
                {
                    job.NextAttemptAt = now;
                    job.UpdatedAt = now;
                    await _context.SaveChangesAsync(ct);
                }
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                return sqlEx.Number is 2601 or 2627;
            return false;
        }


    }

}
