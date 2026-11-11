using GPA.Application.Implementation;
using GPA.Application.Interface;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace GPA.Application.Services
{
    public class PolicyNaicomSyncService : IPolicyNaicomSyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUniwareClient _uniwareClient;
        private readonly IEmailSender _emailSender;
        private readonly IPolicyCertificateService _certService;
        private readonly IIndividualPolicyService _Policyervice;
        private readonly ILogger<PolicyNaicomSyncService> _logger;

        public PolicyNaicomSyncService(
            ApplicationDbContext db,
            IUniwareClient uniwareClient,
            IEmailSender emailSender,
            IPolicyCertificateService certService,
            ILogger<PolicyNaicomSyncService> logger)
        {
            _db = db;
            _uniwareClient = uniwareClient;
            _emailSender = emailSender;
            _certService = certService;
            _logger = logger;
        }

        public async Task SyncMissingNaicomIdsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            //var policies = await _db.IndividualPolicies
            //.Where(p =>
            //    p.PolicyNumber == "OPTGPAS006081/UNI")
            //.OrderBy(p => p.CreatedAt)

            var policies = await _db.IndividualPolicies
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.PolicyNumber) &&
                    !string.IsNullOrWhiteSpace(p.DebitCreditNoteNumber) &&
                    string.IsNullOrWhiteSpace(p.NaicomId) &&
                    p.PolicyStep != null &&
                    p.BillingStep != null &&
                    p.PolicyStep.Status == WorkflowStepStatus.Succeeded &&
                    p.BillingStep.Status == WorkflowStepStatus.Succeeded &&
                    (p.LockedUntilUtc == null || p.LockedUntilUtc < now))
                .OrderBy(p => p.CreatedAt)

            .ToListAsync(cancellationToken);

                if (!policies.Any())
                {
                    _logger.LogInformation("No IndividualPolicy records found with DebitCreditNoteNumber and missing NaicomId.");
                    return;
                }

            _logger.LogInformation("Found {Count} policies to sync from Uniware.", policies.Count);

            foreach (var policy in policies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await SyncSinglePolicyAsync(policy, cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex,
                        "Concurrency conflict while updating policy {PolicyId} with policy number {PolicyNumber}.",
                        policy.Id, policy.PolicyNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed syncing policy {PolicyId} with policy number {PolicyNumber}.",
                        policy.Id, policy.PolicyNumber);
                }
            }
        }

        private async Task SyncSinglePolicyAsync(IndividualPolicy policy, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(policy.PolicyNumber))
                return;

            _logger.LogInformation(
                "Syncing policy details for local policy {PolicyId}, policy number {PolicyNumber}.",
                policy.Id, policy.PolicyNumber);

            var previousNaicomId = policy.NaicomId;
            var naicomJustSynced = false;

            var response = await _uniwareClient.GetPolicyDetailsAsync(policy.PolicyNumber, cancellationToken);

            if (response?.result == null || response.result.Count == 0)
            {
                _logger.LogWarning(
                    "No policy details returned from Uniware for policy number {PolicyNumber}. Raw: {Raw}",
                    policy.PolicyNumber, response?.raw);
                return;
            }

            var details = response.result.FirstOrDefault();
            if (details == null)
                return;

            var changed = false;

            if (!string.IsNullOrWhiteSpace(details.naicom_uid))
            {
                policy.NaicomId = details.naicom_uid;
                changed = true;

                if (string.IsNullOrWhiteSpace(previousNaicomId))
                    naicomJustSynced = true;
            }

            if (TryParseDate(details.effective_date, out var effectiveDate) && policy.StartDate != effectiveDate)
            {
                policy.StartDate = effectiveDate;
                changed = true;
            }

            if (TryParseDate(details.end_date, out var endDate) && policy.EndDate != endDate)
            {
                policy.EndDate = endDate;
                changed = true;
            }

            var firstItem = details.items?.FirstOrDefault();
            if (firstItem != null)
            {
                if (!string.IsNullOrWhiteSpace(firstItem.itemcode) && policy.ItemCode != firstItem.itemcode)
                {
                    policy.ItemCode = firstItem.itemcode;
                    changed = true;
                }

                if (TryParseDecimal(firstItem.sum_assured, out var itemSumAssured) &&
                    policy.SumInsured != itemSumAssured)
                {
                    policy.SumInsured = itemSumAssured;
                    changed = true;
                }

                if (TryParseDate(firstItem.item_strdate, out var itemStartDate) &&
                    policy.StartDate != itemStartDate)
                {
                    policy.StartDate = itemStartDate;
                    changed = true;
                }

                if (TryParseDate(firstItem.item_enddate, out var itemEndDate) &&
                    policy.EndDate != itemEndDate)
                {
                    policy.EndDate = itemEndDate;
                    changed = true;
                }
            }

            var firstDebitNote = details.debit_notes?.FirstOrDefault();
            if (firstDebitNote != null)
            {
                if (!string.IsNullOrWhiteSpace(firstDebitNote.debitnote_no) &&
                    policy.DebitCreditNoteNumber != firstDebitNote.debitnote_no)
                {
                    policy.DebitCreditNoteNumber = firstDebitNote.debitnote_no;

                    if (policy.BillingStep != null)
                        policy.BillingStep.Status = WorkflowStepStatus.Succeeded;

                    changed = true;
                }
            }

            if (!changed)
            {
                _logger.LogInformation(
                    "No changes detected for local policy {PolicyId}, policy number {PolicyNumber}.",
                    policy.Id, policy.PolicyNumber);
                return;
            }

            policy.UpdatedAt = DateTime.UtcNow;

            //_db.IndividualPolicies.Update(policy);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated local policy {PolicyId}. PolicyNumber={PolicyNumber}, NaicomId={NaicomId}, ItemCode={ItemCode}, DCN={DCN}",
                policy.Id,
                policy.PolicyNumber,
                policy.NaicomId,
                policy.ItemCode,
                policy.DebitCreditNoteNumber);

            if (naicomJustSynced )
            {
                try
                {
                    await SendUpdatedPolicyEmailAsync(policy, cancellationToken);

                    policy.UpdatedAt = DateTime.UtcNow;

                    _db.IndividualPolicies.Update(policy);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send updated policy email for policy {PolicyId}, policy number {PolicyNumber}.",
                        policy.Id, policy.PolicyNumber);
                }
            }
        }

        private static bool TryParseDate(string? input, out DateTime value)
        {
            return DateTime.TryParse(input, out value);
        }

        private static bool TryParseDecimal(string? input, out decimal value)
        {
            return decimal.TryParse(input, out value);
        }


        private async Task SendUpdatedPolicyEmailAsync(IndividualPolicy policy, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(policy.Email))
            {
                _logger.LogWarning(
                    "Cannot send updated policy email for policy {PolicyId}: recipient email is missing.",
                    policy.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(policy.PolicyNumber))
                throw new InvalidOperationException("Cannot send updated policy email: PolicyNumber is missing.");

            var fullName = $"{policy.FirstName} {policy.LastName}".Trim();

            var pdfBytes = await _certService.GenerateIndividualPolicyCertificateAsync(policy, cancellationToken);

            var fileName = $"GPA-Policy-{policy.PolicyNumber}.pdf";
            var subject = $"Updated Policy Certificate - {policy.PolicyNumber}";

            var body = $@"
        <div style='font-family:Segoe UI,Arial,sans-serif;background:#f4f7fb;padding:30px'>
          <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.08)'>

            <div style='background:#0d6efd;color:#ffffff;padding:20px;text-align:center'>
              <h2 style='margin:0'>Unitrust Insurance</h2>
              <p style='margin:5px 0 0;font-size:13px;opacity:.9'>Updated Policy Certificate</p>
            </div>

            <div style='padding:25px;color:#333'>
              <p style='font-size:16px'>Hello <b>{fullName}</b>,</p>

              <p>Your policy record has been updated successfully.</p>

              <div style='background:#f1f6ff;border-left:4px solid #0d6efd;padding:15px;margin:20px 0'>
                <p style='margin:0'><b>Policy Number:</b> {policy.PolicyNumber}</p>
                <p style='margin:8px 0 0'><b>NAICOM ID:</b> {policy.NaicomId}</p>
              </div>

              <p>Please find your <b>updated policy certificate</b> attached to this email.</p>

              <p style='margin-top:30px'>
                Regards,<br/>
                <b>Unitrust Insurance</b>
              </p>
            </div>

            <div style='background:#f4f7fb;padding:15px;text-align:center;font-size:12px;color:#777'>
              © {DateTime.UtcNow.Year} Unitrust Insurance. All rights reserved.
            </div>

          </div>
        </div>";

            await _emailSender.SendEmailWithAttachmentAsync(
                to: policy.Email,
                subject: subject,
                htmlMessage: body,
                attachmentBytes: pdfBytes,
                attachmentFileName: fileName,
                contentType: "application/pdf");
        }

    }
}