using GPA.Application.DTO;
using GPA.Application.Interface;
using GPA.Core.Dto.Uniware.Billing;
using GPA.Core.Dto.Uniware.Client;
using GPA.Core.Dto.Uniware.Policy;
using GPA.Core.Models;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GPA.Application.Implementation
{
    public class PolicyCreationService : IPolicyCreationService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const int MaxAttemptsPerStep = 10;
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

        private readonly IEmailSender _emailSender;
        private readonly IPolicyCertificateService _certService;

        private readonly IIndividualPolicyService _individualPolicyService;
        private readonly ICorporatePolicyService _corporatePolicyService;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUniwareClient _uniwareClient;
        private readonly ILogger<PolicyCreationService> _logger;

        public PolicyCreationService(
            IIndividualPolicyService individualPolicyService,
            ICorporatePolicyService corporatePolicyService,
            IPolicyRepository policyRepository,
            IUniwareClient uniwareClient,
            IEmailSender emailSender,  
            IPolicyCertificateService certService,  
            ILogger<PolicyCreationService> logger)
        {
            _individualPolicyService = individualPolicyService;
            _corporatePolicyService = corporatePolicyService;
            _policyRepository = policyRepository;
            _uniwareClient = uniwareClient;
            _emailSender = emailSender;    
            _certService = certService;   
            _logger = logger;
        }


        public async Task<IndividualPolicy> CreateIndividualPolicyAsync(PaymentTransaction tx, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(tx);

            var metadata = DeserializeMetadata(tx);
            ValidateIndividualMetadata(metadata);

            var policy = await EnsureIndividualPolicyExistsAsync(tx, metadata, ct);



             return await RunWorkflowAsync(
                tx: tx,
                policyId: policy.Id,
                metadata: metadata,
                getTrackedPolicyAsync: () => _policyRepository.GetIndividualTrackedAsync(tx.Id, ct),
                saveChangesAsync: () => _policyRepository.SaveChangesAsync(ct),
                tryAcquireLeaseAsync: (token) => _policyRepository.TryAcquireLeaseAsync(policy.Id, "policy-service", token, LeaseDuration, ct),
                releaseLeaseAsync: (token) => _policyRepository.ReleaseLeaseAsync(policy.Id, token, ct),
                executePolicyStepAsync: async (p, now) =>
                {

                    var today = now.Date;

                    string? clientCode = metadata.ClientCode;

                    var uniwareRequest = BuildIndividualPolicyRequest(clientCode, metadata, today);
                    var uniwareResponse = await _uniwareClient.CreatePolicyAsync(uniwareRequest);

                    var result = uniwareResponse?.Result
                                 ?? throw new InvalidOperationException($"Uniware policy creation failed (empty result). Raw: {uniwareResponse?.raw}");

                    if (result.Status != 1)
                        throw new InvalidOperationException($"Uniware policy creation failed: {result.Message}");

                    if (string.IsNullOrWhiteSpace(result.PolicyNumber))
                        throw new InvalidOperationException("Uniware returned success but PolicyNumber is empty.");

                    p.PolicyNumber = result.PolicyNumber;
                    p.ItemCode = result.ItemCode;

                },
                executeBillingStepAsync: async (p, now) =>
                {
                    if (string.IsNullOrWhiteSpace(p.PolicyNumber))
                        throw new InvalidOperationException("Cannot generate billing: PolicyNumber is missing.");

                    var billing = await _uniwareClient.GenerateBillingAsync(p.PolicyNumber!, ct);

                    if (billing?.result?.status == 1)
                    {
                        p.DebitCreditNoteNumber = billing.result.dcnumb;
                        p.NaicomId = billing.result.naicom_id;
                        return StepResult.Success();
                    }

                    return StepResult.Failed(billing?.raw ?? "Billing failed");
                },
                executeReceiptStepAsync: async (p, now) =>
                {

                    if (string.IsNullOrWhiteSpace(p.PolicyNumber))
                        throw new InvalidOperationException("Cannot create receipt: PolicyNumber is missing.");

                    if (string.IsNullOrWhiteSpace(p.DebitCreditNoteNumber))
                        throw new InvalidOperationException("Cannot create receipt: DebitCreditNoteNumber (idocnum) is missing.");

                    var receiptRequest = new CreateReceiptRequest
                    {
                        Polnum = p.PolicyNumber!,
                        PaymentRef = tx.Reference ?? tx.Id.ToString(),
                        TransactionDate = now.Date.ToString("yyyy-MM-dd"),
                        Amount = tx.Amount,
                        Idocnum = p.DebitCreditNoteNumber!,
                        Bank = "B00331" 
                    };

                    var receipt = await _uniwareClient.CreateReceiptAsync(receiptRequest, ct);

                    if (receipt?.result?.status == true)
                    {
                        p.ReceiptNumber = receipt.result.refnum;
                        return StepResult.Success();
                    }

                    return StepResult.Failed(receipt?.raw ?? "Receipt creation failed");
                },
                ct: ct
            );
        }

        public async Task<CorporatePolicy> CreateCorporatePolicyAsync(PaymentTransaction tx, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(tx);

            var metadata = DeserializeMetadata(tx);
            ValidateCorporateMetadata(metadata);

            var policy = await EnsureCorporatePolicyExistsAsync(tx, metadata, ct);

            return await RunWorkflowAsync(
                tx: tx,
                policyId: policy.Id,
                metadata: metadata,
                getTrackedPolicyAsync: () => _policyRepository.GetCorporateTrackedAsync(tx.Id, ct),
                saveChangesAsync: () => _policyRepository.SaveChangesAsync(ct),
                tryAcquireLeaseAsync: (token) => _policyRepository.TryAcquireLeaseAsync(policy.Id, "policy-service", token, LeaseDuration, ct),
                releaseLeaseAsync: (token) => _policyRepository.ReleaseLeaseAsync(policy.Id, token, ct),
                executePolicyStepAsync: async (p, now) =>
                {
                    var today = now.Date;

                    var clientCode = await _uniwareClient.SearchClientAsync(metadata.Email!, null);
                    if (string.IsNullOrWhiteSpace(clientCode))
                    {
                        clientCode = await _uniwareClient.CreateClientAsync(new CreateClientRequest
                        {
                            email = metadata.Email,
                            office_addr = metadata.CompanyAddress!,
                            insured = metadata.CompanyName!,
                            nin = ""
                        });

                        if (string.IsNullOrWhiteSpace(clientCode))
                            throw new InvalidOperationException("Failed to create client in Uniware (empty clientCode).");
                    }

                    decimal insuredVal = ParseDecimalOrThrow(metadata.TotalAmount, nameof(metadata.TotalAmount));

                    var uniwareRequest = BuildCorporatePolicyRequest(clientCode!, metadata, today, insuredVal);
                    var uniwareResponse = await _uniwareClient.CreatePolicyAsync(uniwareRequest);

                    var result = uniwareResponse?.Result
                                 ?? throw new InvalidOperationException($"Uniware policy creation failed (empty result). Raw: {uniwareResponse?.raw}");

                    if (result.Status != 1)
                        throw new InvalidOperationException($"Uniware policy creation failed: {result.Message}");

                    if (string.IsNullOrWhiteSpace(result.PolicyNumber))
                        throw new InvalidOperationException("Uniware returned success but PolicyNumber is empty.");

                    p.PolicyNumber = result.PolicyNumber;
                    p.ItemCode = result.ItemCode;
                },
                executeBillingStepAsync: async (p, now) =>
                {
                    if (string.IsNullOrWhiteSpace(p.PolicyNumber))
                        throw new InvalidOperationException("Cannot generate billing: PolicyNumber is missing.");

                    var billing = await _uniwareClient.GenerateBillingAsync(p.PolicyNumber!, ct);

                    if (billing?.result?.status == 1)
                    {
                        p.DebitCreditNoteNumber = billing.result.dcnumb;
                        return StepResult.Success();
                    }

                    return StepResult.Failed(billing?.raw ?? "Billing failed");
                },
                executeReceiptStepAsync: async (p, now) =>
                {
                    if (string.IsNullOrWhiteSpace(p.PolicyNumber))
                        throw new InvalidOperationException("Cannot create receipt: PolicyNumber is missing.");

                    if (string.IsNullOrWhiteSpace(p.DebitCreditNoteNumber))
                        throw new InvalidOperationException("Cannot create receipt: DebitCreditNoteNumber (idocnum) is missing.");

                    var receiptRequest = new CreateReceiptRequest
                    {
                        Polnum = p.PolicyNumber!,
                        PaymentRef = tx.Reference ?? tx.Id.ToString(),
                        TransactionDate = now.Date.ToString("yyyy-MM-dd"),
                        Amount = tx.Amount,
                        Idocnum = p.DebitCreditNoteNumber!,
                        Bank = "B00331" // TODO: move to config
                    };

                    var receipt = await _uniwareClient.CreateReceiptAsync(receiptRequest, ct);

                    if (receipt?.result?.status == true)
                    {
                        p.ReceiptNumber = receipt.result.refnum;
                        return StepResult.Success();
                    }

                    return StepResult.Failed(receipt?.raw ?? "Receipt creation failed");
                },
                ct: ct
            );
        }

        private async Task<TPolicy> RunWorkflowAsync<TPolicy>(
            PaymentTransaction tx,
            Guid policyId,
            PaymentMetadata metadata,
            Func<Task<TPolicy?>> getTrackedPolicyAsync,
            Func<Task> saveChangesAsync,
            Func<Guid, Task<bool>> tryAcquireLeaseAsync,
            Func<Guid, Task> releaseLeaseAsync,
            Func<TPolicy, DateTime, Task> executePolicyStepAsync,
            Func<TPolicy, DateTime, Task<StepResult>> executeBillingStepAsync,
            Func<TPolicy, DateTime, Task<StepResult>> executeReceiptStepAsync,
            CancellationToken ct)
            where TPolicy : PolicyBase
        {
            var correlationId = tx.Reference ?? tx.Id.ToString();
            var lockToken = Guid.NewGuid();

            bool acquired = false;
            TPolicy? policy = null;

            async Task<bool> RunStepOrBreakAsync(
                WorkflowStepState step,
                Func<DateTime, Task> doWorkAsync,
                string defaultError )
            {


                var now = DateTime.UtcNow;

                if (step.Status == WorkflowStepStatus.Succeeded)
                    return true;


                if (IsBlocked(step, now))
                    return false;

                if (!ShouldRun(step, now))
                    return false;


                MarkInProgress(step, now);
                policy!.UpdatedAt = now;
                await saveChangesAsync();

                try
                {
                    // do step work
                    await doWorkAsync(now);

                    now = DateTime.UtcNow;
                    MarkSucceeded(step, now);
                    policy!.UpdatedAt = now;
                    await saveChangesAsync();


                    return step.Status == WorkflowStepStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    now = DateTime.UtcNow;
                    MarkFailed(step, string.IsNullOrWhiteSpace(ex.Message) ? defaultError : ex.Message, now);
                    policy!.UpdatedAt = now;
                    await saveChangesAsync();
                    return false;
                }
            }

            try
            {
                async Task<TPolicy> FinishAsync(TPolicy p)
                {
                    if (p is IndividualPolicy ind)
                        await TrySendFirstTrialEmailOnceAsync(ind, tx, metadata, saveChangesAsync, ct);

                    return p;
                }

                acquired = await tryAcquireLeaseAsync(lockToken);

                if (!acquired)
                {
                    policy = await getTrackedPolicyAsync() ?? throw new InvalidOperationException("Policy not found.");
                    return await FinishAsync(policy);
                }


                policy = await getTrackedPolicyAsync() ?? throw new InvalidOperationException("Policy not found after acquiring lease.");




                EnsureSteps(policy);

                var now = DateTime.UtcNow;
                if (policy.PaymentStep.Status == WorkflowStepStatus.NotStarted)
                {
                    MarkSucceeded(policy.PaymentStep, now);
                    policy.UpdatedAt = now;
                    await saveChangesAsync();
                }


                var policyOk = await RunStepOrBreakAsync(
                    policy.PolicyStep,
                    doWorkAsync: (t) => executePolicyStepAsync(policy, t),
                    defaultError: "Policy creation failed");

                if (!policyOk)
                {

                    return await FinishAsync(policy);
                }

                if (policy.BillingStep.Status != WorkflowStepStatus.Succeeded && !string.IsNullOrWhiteSpace(policy.PolicyNumber))
                {
                    try
                    {
                        var details = await _uniwareClient.GetPolicyDetailsAsync(policy.PolicyNumber!, ct);
                        var policyDetails = details?.result?.FirstOrDefault();
                        var firstDebitNote = policyDetails?.debit_notes?.FirstOrDefault();

                        var changed = false;

                        if (policyDetails != null)
                        {
                            if (!string.IsNullOrWhiteSpace(policyDetails.naicom_uid) &&
                                policy.NaicomId != policyDetails.naicom_uid)
                            {
                                policy.NaicomId = policyDetails.naicom_uid;
                                changed = true;
                            }

                            if (firstDebitNote != null && !string.IsNullOrWhiteSpace(firstDebitNote.debitnote_no))
                            {
                                if (policy.DebitCreditNoteNumber != firstDebitNote.debitnote_no)
                                {
                                    policy.DebitCreditNoteNumber = firstDebitNote.debitnote_no;
                                    changed = true;
                                }

                                var nowSync = DateTime.UtcNow;
                                MarkSucceeded(policy.BillingStep, nowSync);
                                policy.UpdatedAt = nowSync;
                                changed = true;
                            }

                            if (changed)
                                await saveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to sync billing details from Uniware policy details. policyId={PolicyId} policyNumber={PolicyNumber}",
                            policy.Id, policy.PolicyNumber);
                    }
                }

                // --------------------
                // BILLING STEP (gate)
                // --------------------
                var billingOk = await RunStepOrBreakAsync(
                    policy.BillingStep,
                    doWorkAsync: async (t) =>
                    {
                        var res = await executeBillingStepAsync(policy, t);
                        if (!res.Ok)
                            throw new InvalidOperationException(res.Error ?? "Billing failed");
                    },
                    defaultError: "Billing failed");

                if (!billingOk)
                    return await FinishAsync(policy);


                // --------------------
                // RECEIPT STEP (gate)
                // --------------------
                var receiptOk = await RunStepOrBreakAsync(
                    policy.ReceiptStep,
                    doWorkAsync: async (t) =>
                    {
                        var res = await executeReceiptStepAsync(policy, t);
                        if (!res.Ok)
                            throw new InvalidOperationException(res.Error ?? "Receipt creation failed");
                    },
                    defaultError: "Receipt creation failed");

                // Whether receipt succeeded or not, we short-circuit on failure already,
                // so reaching here means either it succeeded, or it was already succeeded.
                return await FinishAsync(policy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Policy workflow failed. policyId={PolicyId} txId={TxId} ref={Ref}",
                    policyId, tx.Id, correlationId);

                // Best-effort persist failure if a step is in-progress
                if (policy != null)
                {
                    try
                    {
                        EnsureSteps(policy);
                        var now2 = DateTime.UtcNow;

                        if (policy.PolicyStep.Status == WorkflowStepStatus.InProgress)
                            MarkFailed(policy.PolicyStep, ex.Message, now2);
                        else if (policy.BillingStep.Status == WorkflowStepStatus.InProgress)
                            MarkFailed(policy.BillingStep, ex.Message, now2);
                        else if (policy.ReceiptStep.Status == WorkflowStepStatus.InProgress)
                            MarkFailed(policy.ReceiptStep, ex.Message, now2);

                        policy.UpdatedAt = now2;
                        await saveChangesAsync();
                    }
                    catch (Exception persistEx)
                    {
                        _logger.LogError(persistEx, "Failed to persist workflow failure state. policyId={PolicyId}", policyId);
                    }
                }

                throw;
            }
            finally
            {
                if (acquired)
                {
                    try
                    {
                        await releaseLeaseAsync(lockToken);
                    }
                    catch (Exception releaseEx)
                    {
                        _logger.LogError(releaseEx, "Failed to release lease. policyId={PolicyId}", policyId);
                    }
                }
            }
        }

        private async Task<IndividualPolicy> EnsureIndividualPolicyExistsAsync(PaymentTransaction tx, PaymentMetadata metadata, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            if (await _policyRepository.GetByPaymentTransactionIdAsync(tx.Id) is IndividualPolicy existing)
                return existing;

            var units = metadata.Units;

            const decimal sumInsuredPerUnit = 1_000_000m;
            const decimal premiumPerUnit = 1_500m;

            string firstName = "";
            string lastName = "";

            if (!string.IsNullOrWhiteSpace(metadata.ClientCode))
            {
                var client = await _uniwareClient.GetClientAsync(metadata.ClientCode, ct);

                var fullName = client?.Result.Record.FirstOrDefault()?.Name?.Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                {

                    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    firstName = parts.FirstOrDefault() ?? "";
                    lastName = parts.Length > 1
                        ? string.Join(" ", parts.Skip(1))
                        : "";

                }
            }

            var policy = new IndividualPolicy
            {
                PaymentTransactionId = tx.Id,
                FirstName = firstName,
                LastName = lastName,
                NIN = "",
                PhoneNumber = metadata.Phone!,
                ClientCode = metadata.ClientCode,
                Email = metadata.Email!,
                NextOfKinName = metadata.NextOfKinName,
                NextOfKinPhone = metadata.NextOfKinPhone,
                Units = units,
                UnitPrice = premiumPerUnit,
                SumInsured = units * premiumPerUnit,
                StartDate = today,
                EndDate = today.AddYears(1).AddDays(-1),
                CreatedAt = now,
                UpdatedAt = now
            };

            EnsureSteps(policy);
            MarkSucceeded(policy.PaymentStep, now);

            try
            {
                await _individualPolicyService.CreateAsync(policy);
                return policy;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return (IndividualPolicy)(await _policyRepository.GetByPaymentTransactionIdAsync(tx.Id)
                    ?? throw new InvalidOperationException("Policy exists but could not be loaded."));
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        private async Task<CorporatePolicy> EnsureCorporatePolicyExistsAsync(PaymentTransaction tx, PaymentMetadata metadata, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            if (await _policyRepository.GetByPaymentTransactionIdAsync(tx.Id) is CorporatePolicy existing)
                return existing;

            decimal insuredVal = ParseDecimalOrThrow(metadata.TotalAmount, nameof(metadata.TotalAmount));

            var policy = new CorporatePolicy
            {
                PaymentTransactionId = tx.Id,
                CompanyName = metadata.CompanyName!,
                CompanyEmail = metadata.Email!,
                CompanyAddress = metadata.CompanyAddress!,
                TotalEmployees = metadata.Beneficiaries?.Count ?? 0,
                TotalSumInsured = insuredVal,
                CreatedAt = now,
                UpdatedAt = now
            };

            EnsureSteps(policy);
            MarkSucceeded(policy.PaymentStep, now);

            if (metadata.Beneficiaries != null)
            {
                foreach (var b in metadata.Beneficiaries)
                {
                    if (string.IsNullOrWhiteSpace(b.Name))
                        continue;

                    var units = 0;
                    if (b.Units != null)
                    {
                        try { units = Convert.ToInt32(b.Units); } catch { units = 0; }
                    }

                    policy.Beneficiaries.Add(new CorporatePolicyBeneficiary
                    {
                        CorporatePolicy = policy,
                        FullName = b.Name,
                        PhoneNumber = b.Phone,
                        units = units
                    });
                }
            }

            try
            {
                await _corporatePolicyService.CreateAsync(policy);
                return policy;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return (CorporatePolicy)(await _policyRepository.GetByPaymentTransactionIdAsync(tx.Id)
                    ?? throw new InvalidOperationException("Policy exists but could not be loaded."));
            }
        }

        private static bool ShouldRun(WorkflowStepState step, DateTime now)
        {
            if (step.Status == WorkflowStepStatus.Succeeded || step.Status == WorkflowStepStatus.RequiresManualReview)
                return false;

            if (step.Status == WorkflowStepStatus.InProgress)
            {
                if (step.LastAttemptAt.HasValue && step.LastAttemptAt.Value.Add(LeaseDuration) < now)
                    return true;

                return false;
            }

            if (step.Status == WorkflowStepStatus.Failed)
            {
                if (step.NextRetryAt.HasValue && step.NextRetryAt.Value > now) return false;
                if (step.AttemptCount >= MaxAttemptsPerStep) return false;
                return true;
            }

            return step.Status == WorkflowStepStatus.NotStarted;
        }

        private static bool IsBlocked(WorkflowStepState step, DateTime now)
        {

            if (step.Status == WorkflowStepStatus.RequiresManualReview)
                return true;

            if (step.Status == WorkflowStepStatus.Failed && step.NextRetryAt.HasValue && step.NextRetryAt.Value > now)
                return true;

            if (step.Status == WorkflowStepStatus.Failed && step.AttemptCount >= MaxAttemptsPerStep)
                return true;

            return false;

        }

        private static void MarkInProgress(WorkflowStepState step, DateTime now)
        {
            step.Status = WorkflowStepStatus.InProgress;
            step.AttemptCount++;
            step.LastAttemptAt = now;
            step.LastError = null;
        }

        private static void MarkSucceeded(WorkflowStepState step, DateTime now)
        {
            step.Status = WorkflowStepStatus.Succeeded;
            step.SucceededAt = now;
            step.NextRetryAt = null;
            step.LastError = null;
        }

        private static void MarkFailed(WorkflowStepState step, string error, DateTime now)
        {
            step.LastError = error;

            if (step.AttemptCount >= MaxAttemptsPerStep)
            {
                step.Status = WorkflowStepStatus.RequiresManualReview;
                step.NextRetryAt = null;
                return;
            }

            step.Status = WorkflowStepStatus.Failed;
            step.NextRetryAt = ComputeNextRetryAt(step.AttemptCount, now);
        }

        private static DateTime ComputeNextRetryAt(int attemptCount, DateTime now)
        {


            var minutes = attemptCount switch
            {
                1 => 1,
                2 => 2,
                3 => 5,
                4 => 10,
                5 => 20,
                _ => 60
            };

            return now.AddMinutes(minutes);
        }

        private static void EnsureSteps(PolicyBase policy)
        {
            policy.PaymentStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.PolicyStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.BillingStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.ReceiptStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
            policy.EmailStep ??= new WorkflowStepState { Status = WorkflowStepStatus.NotStarted };
        }

        private static CreatePolicyRequest BuildIndividualPolicyRequest(string clientCode, PaymentMetadata metadata, DateTime today)
        {
            var start = today.ToString("yyyy-MM-dd");
            var end = today.AddYears(1).AddDays(-1).ToString("yyyy-MM-dd");

            var insuredVal = metadata.Units * 1000000;
            var desiredPremium = metadata.Units * 1500m;

            const string branch = "0574";
            const string polagency = "112109";
            const string polclass = "2";
            const string risktype = "01125";
            const string bsntype = "00715";
            const string coverType = "00400";

            return new CreatePolicyRequest
            {
                policy_details = new PolicyDetails
                {
                    client_code = clientCode,
                    branch = branch,
                    polagency = polagency,
                    polclass = polclass,
                    risktype = risktype,
                    bsntype = bsntype,
                    issuedate = start,
                    efcdate = start,
                    clo = metadata.CloCode?? "",
                },
                item_details = new List<ItemDetails>
                {
                    new ItemDetails
                    {
                        itemdesc = $"Individual Personal Accident Policy for client code {metadata.ClientCode}",
                        risktype = risktype,
                        cover_type = coverType,
                        entdate = start,
                        bsntype = bsntype,
                        insuredval = insuredVal,
                        desiredprem = desiredPremium,
                        item_strdate = start,
                        item_enddate = end
                    }
                }
            };
        }

        private static CreatePolicyRequest BuildCorporatePolicyRequest(string clientCode, PaymentMetadata metadata, DateTime today, decimal insuredVal)
        {
            var start = today.ToString("yyyy-MM-dd");
            var end = today.AddYears(1).AddDays(-1).ToString("yyyy-MM-dd");
            var actualInsuredVal = metadata.Units * 1000000;  

            return new CreatePolicyRequest
            {
                policy_details = new PolicyDetails
                {
                    client_code = clientCode,
                    branch = "0574",
                    polagency = "112109",
                    polclass = "2",
                    risktype = "01125",
                    bsntype = "00715",
                    issuedate = start,
                    efcdate = start
                },
                item_details = new List<ItemDetails>
                {
                    new ItemDetails
                    {
                        itemdesc = $"Corporate Group Personal Accident Policy for {metadata.CompanyName}",
                        risktype = "01125",
                        cover_type = "00401",
                        entdate = start,
                        bsntype = "00715",
                        insuredval = actualInsuredVal,
                        desiredprem = insuredVal,
                        item_strdate = start,
                        item_enddate = end
                    }
                }
            };
        }

        private static PaymentMetadata DeserializeMetadata(PaymentTransaction tx)
        {
            return JsonSerializer.Deserialize<PaymentMetadata>(tx.Metadata ?? "{}", JsonOpts)
                   ?? throw new InvalidOperationException("Missing payment metadata");
        }

        private static void ValidateIndividualMetadata(PaymentMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.Email))
                throw new InvalidOperationException("Payment metadata missing Email.");
            //if (string.IsNullOrWhiteSpace(metadata.ClientCode))
            //    throw new InvalidOperationException("Payment metadata missing clientcode.");
            if (string.IsNullOrWhiteSpace(metadata.Phone))
                throw new InvalidOperationException("Payment metadata missing Phone Number.");
        }

        private static void ValidateCorporateMetadata(PaymentMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.Email))
                throw new InvalidOperationException("Payment metadata missing Email.");
            if (string.IsNullOrWhiteSpace(metadata.CompanyName))
                throw new InvalidOperationException("Payment metadata missing CompanyName.");
            if (string.IsNullOrWhiteSpace(metadata.CompanyAddress))
                throw new InvalidOperationException("Payment metadata missing CompanyAddress.");
            if (string.IsNullOrWhiteSpace(metadata.ClientCode))
                throw new InvalidOperationException("Payment metadata missing client code.");
        }

        private static decimal ParseDecimalOrThrow(object? value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"Payment metadata missing {fieldName}.");

            if (value is decimal d) return d;
            if (value is int i) return i;
            if (value is long l) return l;

            if (decimal.TryParse(value.ToString(), out var result))
                return result;

            throw new InvalidOperationException($"Payment metadata {fieldName} is not a valid decimal: '{value}'.");
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                return sqlEx.Number is 2601 or 2627;
            return false;
        }

        private static PolicyEmailKind DecideEmailKindAfterFirstTrial(PolicyBase p)
        {
            var succeeded =
                p.PolicyStep?.Status == WorkflowStepStatus.Succeeded &&
                p.BillingStep?.Status == WorkflowStepStatus.Succeeded &&
                p.ReceiptStep?.Status == WorkflowStepStatus.Succeeded;

            return succeeded ? PolicyEmailKind.PolicyCertificate : PolicyEmailKind.PaymentProof;
        }

        private async Task TrySendFirstTrialEmailOnceAsync(IndividualPolicy policy, PaymentTransaction tx, PaymentMetadata metadata,Func<Task> saveChangesAsync, CancellationToken ct)
        {

            EnsureSteps(policy);

            if (policy.EmailStep!.Status == WorkflowStepStatus.Succeeded)
                return;

            var now = DateTime.UtcNow;

            if (policy.EmailKind == PolicyEmailKind.NotDecided)
            {
                policy.EmailKind = DecideEmailKindAfterFirstTrial(policy);
                policy.EmailKindDecidedAt = now;
                policy.UpdatedAt = now;
                await saveChangesAsync();
            }

            if (IsBlocked(policy.EmailStep!, now) || !ShouldRun(policy.EmailStep!, now))
                return;

            MarkInProgress(policy.EmailStep!, now);
            policy.UpdatedAt = now;
            await saveChangesAsync();

            try
            {
                var to = policy.Email ?? metadata.Email;
                if (string.IsNullOrWhiteSpace(to))
                    throw new InvalidOperationException("Cannot send email: recipient email missing.");

                var fullName = $"{policy.FirstName} {policy.LastName}".Trim();

                byte[] pdfBytes;
                string fileName;
                string subject;
                string body;

                if (policy.EmailKind == PolicyEmailKind.PolicyCertificate)
                {
                    if (string.IsNullOrWhiteSpace(policy.PolicyNumber))
                        throw new InvalidOperationException("Cannot send certificate: PolicyNumber missing.");

                    pdfBytes = await _certService.GenerateIndividualPolicyCertificateAsync(policy, ct);
                    fileName = $"GPA-Policy-{policy.PolicyNumber}.pdf";
                    subject = $"Your Policy Certificate - {policy.PolicyNumber}";
                    body = $@"
                        <div style='font-family:Segoe UI,Arial,sans-serif;background:#f4f7fb;padding:30px'>
                          <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08)'>

                            <div style='background:#0d6efd;color:#ffffff;padding:20px;text-align:center'>
                              <h2 style='margin:0'>Unitrust Insurance</h2>
                              <p style='margin:5px 0 0;font-size:13px;opacity:.9'>Policy Certificate</p>
                            </div>

                            <div style='padding:25px;color:#333'>
                              <p style='font-size:16px'>Hello <b>{fullName}</b>,</p>

                              <p>Your policy has been created successfully.</p>

                              <div style='background:#f1f6ff;border-left:4px solid #0d6efd;padding:15px;margin:20px 0'>
                                <b>Policy Number:</b> {policy.PolicyNumber}
                              </div>

                              <p>Please find your <b>policy certificate</b> attached to this email.</p>

                              <p style='margin-top:30px'>
                                Regards,<br/>
                                <b>Unitrust Insurance</b>
                              </p>
                            </div>

                            <div style='background:#f4f7fb;padding:20px;text-align:center;font-size:12px;color:#777'>
  
                              <div style='border-top:1px solid #ddd;padding-top:15px'>
                                <strong>Need help?</strong><br/>
                                📞 +234700UNITRUST &nbsp; | &nbsp;
                                ✉ customerservice@unitrustinsurance.com &nbsp; | &nbsp;
                                🌐 www.unitrustinsurance.com
                                <br/><br/>
                                © {DateTime.UtcNow.Year} Unitrust Insurance. All rights reserved.
                              </div>

                            </div>

                          </div>
                        </div>";

                }
                else
                {

                    var proofData = new PaymentProofData
                    {
                        TransactionReference = tx.Reference ?? tx.Id.ToString(),
                        Amount = tx.Amount,
                        PaymentDate = tx.UpdatedAt,
                        metadata = metadata
                    };

                    pdfBytes = await _certService.GeneratePaymentProofCertificateAsync(proofData, ct);
                    fileName = $"GPA-Payment-Proof-{proofData.TransactionReference}.pdf";
                    subject = $"Payment Confirmation - {proofData.TransactionReference}";
                    body = $@" 
                        <div style='font-family:Segoe UI,Arial,sans-serif;background:#f4f7fb;padding:30px'>
                          <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08)'>

                            <div style='background:#0d6efd;color:#ffffff;padding:20px;text-align:center'>
                              <h2 style='margin:0'>Unitrust Insurance</h2>
                              <p style='margin:5px 0 0;font-size:13px;opacity:.9'>Payment Confirmation</p>
                            </div>

                            <div style='padding:25px;color:#333'>
                              <p style='font-size:16px'>Hello <b>{fullName}</b>,</p>

                              <p>Your payment has been received successfully.</p>

                              <div style='background:#f1f6ff;border-left:4px solid #0d6efd;padding:15px;margin:20px 0'>
                                <b>Reference:</b> {proofData.TransactionReference}
                              </div>

                              <p>Your policy document is being processed.  
                              Please find your <b>payment confirmation</b> attached.</p>

                              <p style='margin-top:30px'>
                                Regards,<br/>
                                <b>Unitrust Insurance</b>
                              </p>
                            </div>

                            <div style='background:#f4f7fb;padding:20px;text-align:center;font-size:12px;color:#777'>
  
                              <div style='border-top:1px solid #ddd;padding-top:15px'>
                                <strong>Need help?</strong><br/>
                                📞 +234700UNITRUST &nbsp; | &nbsp;
                                ✉ customerservice@unitrustinsurance.com &nbsp; | &nbsp;
                                🌐 www.unitrustinsurance.com
                                <br/><br/>
                                © {DateTime.UtcNow.Year} Unitrust Insurance. All rights reserved.
                              </div>

                            </div>
                          </div>
                        </div>";

                }

                await _emailSender.SendEmailWithAttachmentAsync(
                    to: to!,
                    subject: subject,
                    htmlMessage: body,
                    attachmentBytes: pdfBytes,
                    attachmentFileName: fileName,
                    contentType: "application/pdf"
                );

                var t2 = DateTime.UtcNow;
                MarkSucceeded(policy.EmailStep!, t2);
                policy.EmailSentAt = t2;
                policy.UpdatedAt = t2;
                await saveChangesAsync();

            }
            catch (Exception ex)
            {
                var t2 = DateTime.UtcNow;
                MarkFailed(policy.EmailStep!, ex.Message, t2); 
                policy.UpdatedAt = t2;
                await saveChangesAsync();

                _logger.LogError(ex, "Email send failed (first-trial locked). policyId={PolicyId} txId={TxId}", policy.Id, tx.Id);
            }
        }

        private readonly record struct StepResult(bool Ok, string? Error)
        {
            public static StepResult Success() => new(true, null);
            public static StepResult Failed(string error) => new(false, error);
        }

    }
}
