using GPA.Application.DTO;
using GPA.Application.Interface;
using GPA.Core.Dto.Uniware.Client;
using GPA.Core.Models.Policy;
using GPA.Infrastructure.HttpClients;
using GPA.Infrastructure.Persistence;
using GPA.Infrastructure.Persistence.Interface;
using iText.Forms;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Text.Json;
using Path = System.IO.Path;

namespace GPA.Application.Implementation
{
    public interface IPolicyCertificateService
    {
        Task<byte[]> GenerateIndividualPolicyCertificateAsync(IndividualPolicy policy, CancellationToken ct);
        Task<byte[]> GeneratePaymentProofCertificateAsync(PaymentProofData data, CancellationToken ct);
        Task SendPolicyEmail(Guid id, CancellationToken ct);
    }

    public class PolicyCertificateService : IPolicyCertificateService
    {
        private readonly IUniwareClient _uniwareClient;
        private readonly IEmailSender _emailSender;
        private readonly IIndividualPolicyService _PolicyService;
        private readonly ITemplatePathProvider _paths;
        private readonly IPaymentTransactionRepository _paymentTransactionReadService;
        private readonly ApplicationDbContext _context;



        public PolicyCertificateService(
                IUniwareClient uniwareClient,
                IEmailSender emailSender,
                ITemplatePathProvider paths,
                IIndividualPolicyService PolicyService,
                ApplicationDbContext context,
                IPaymentTransactionRepository paymentTransactionReadService
            )
        {
            _uniwareClient = uniwareClient;
            _paths = paths;
            _emailSender = emailSender;
            _paymentTransactionReadService = paymentTransactionReadService;
            _PolicyService = PolicyService;
            _context = context;
        }

        public async Task<byte[]> GenerateIndividualPolicyCertificateAsync(IndividualPolicy policy, CancellationToken ct)
        {

            try
            {

                var updatedpolicy = await _PolicyService.GetById(policy.Id);

                var templatePath = _paths.GetPolicyTemplatePath();

                if (!File.Exists(templatePath))
                    throw new FileNotFoundException($"Policy certificate template not found at: {templatePath}");

                var transaction = await _paymentTransactionReadService
                    .GetByIdAsync(updatedpolicy.PaymentTransactionId, ct);

                if (transaction == null)
                    throw new InvalidOperationException("Payment transaction not found.");

                var (pdDeath, accMed, _) = ComputeBenefitsFromUnits(updatedpolicy.Units);


                string fullname = string.Empty; 
                fullname = $"{updatedpolicy.FirstName} {updatedpolicy.LastName}".Trim();    

                if (string.IsNullOrWhiteSpace(fullname))
                {
                    if (!string.IsNullOrWhiteSpace(updatedpolicy.ClientCode))
                    {
                        var client = await _uniwareClient.GetClientAsync(updatedpolicy.ClientCode, ct);

                        var fullName = client?.Result.Record.FirstOrDefault()?.Name?.Trim();

                        if (!string.IsNullOrWhiteSpace(fullName))
                        {
                            fullname = fullName;
                        }
                    }
                }

                fullname = fullname.Trim();

                

                var beneficiary = updatedpolicy.NextOfKinName ?? "-";
                var contact = updatedpolicy.NextOfKinPhone ?? "-";

                using var outputStream = new MemoryStream();
                using var reader = new PdfReader(templatePath);
                using var writer = new PdfWriter(outputStream, new WriterProperties()
                    .SetCompressionLevel(CompressionConstants.BEST_COMPRESSION)
                    .SetFullCompressionMode(true));
                writer.SetSmartMode(true);
                using var pdfDoc = new PdfDocument(reader, writer);

                var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
                var fields = form.GetAllFormFields();

                void SetIfExists(string fieldName, string? value)
                {
                    if (string.IsNullOrWhiteSpace(value)) return;
                    if (fields.TryGetValue(fieldName, out var f))
                        f.SetValue(value);
                }

                SetIfExists("Insured", fullname);
                SetIfExists("Insured Name", fullname);
                SetIfExists("Beneficiary", beneficiary);

                SetIfExists("Contact", contact);

                SetIfExists("Policy No", updatedpolicy.PolicyNumber);

                SetIfExists("NID", updatedpolicy.NaicomId);
                SetIfExists("NaicomID", updatedpolicy.NaicomId);
                SetIfExists("NaicomId", updatedpolicy.NaicomId);

                SetIfExists("Accidental Death", Naira(pdDeath));
                SetIfExists("Permanent Disability", Naira(pdDeath));
                SetIfExists("Medical Benefit", Naira(accMed));

                SetIfExists("StartDate", $"{updatedpolicy.StartDate:dd MMM yyyy}");
                SetIfExists("EndDate", $"{updatedpolicy.EndDate:dd MMM yyyy}");
                SetIfExists("Date", $"{DateTime.UtcNow:dd MMM yyyy}");
                SetIfExists("SignedBy", "Managing Director/CEO");

                form.FlattenFields();
                pdfDoc.Close();

                var bytes = outputStream.ToArray();

                var header = System.Text.Encoding.ASCII.GetString(bytes.Take(5).ToArray());
                if (header != "%PDF-")
                    throw new InvalidOperationException($"Generated file is not a valid PDF. Header={header}, Len={bytes.Length}");

                return bytes;
            }
            catch (FileNotFoundException)
            {
                throw; 
            }
            catch (OperationCanceledException)
            {
                throw; // respect cancellation
            }
            catch (Exception ex)
            {
                // TODO: add logger here if available
                // _logger.LogError(ex, "Failed to generate individual policy certificate for PolicyId {PolicyId}", policy.Id);

                throw new ApplicationException("An error occurred while generating the policy certificate PDF.", ex);
            }
        }


        public async Task<byte[]> GeneratePaymentProofCertificateAsync(PaymentProofData data, CancellationToken ct)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var templatePath = _paths.GetPaymentProofTemplatePath();
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Payment proof template not found at: {templatePath}");

            _ = await _paymentTransactionReadService.GetByReferenceAsync(data.TransactionReference, ct);

            using var outputStream = new MemoryStream();
            using var reader = new PdfReader(templatePath);
            using var writer = new PdfWriter(outputStream, new WriterProperties());
            using var pdfDoc = new PdfDocument(reader, writer);

            var form = PdfAcroForm.GetAcroForm(pdfDoc, true);
            var fields = form.GetAllFormFields();

            //if (fields.TryGetValue("Insured", out var insuredField))
            //    insuredField.SetValue($"{data.metadata.FirstName} {data.metadata.LastName}");

            //if (fields.TryGetValue("Insured Name", out var insuredNameField))
            //    insuredNameField.SetValue($"{data.metadata.FirstName} {data.metadata.LastName}");

            if (fields.TryGetValue("Address", out var addressField))
                addressField.SetValue("*************");

            if (fields.TryGetValue("Contact", out var contactField))
                contactField.SetValue(data.metadata.Email);

            if (fields.TryGetValue("Beneficiary", out var beneficiaryField))
                beneficiaryField.SetValue("*************");

            if (fields.TryGetValue("Policy No", out var policyNoField))
                policyNoField.SetValue("*************");

            if (fields.TryGetValue("Period", out var periodField))
                periodField.SetValue("*************");

            CleanFieldAppearance(form);
            form.FlattenFields();

            AddWatermarkToAllPages(pdfDoc, "PAYMENT CONFIRMATION ONLY\nNOT A VALID CERTIFICATE");

            pdfDoc.Close();
            return outputStream.ToArray();
        }

        private static void AddWatermarkToAllPages(PdfDocument pdfDoc, string watermarkText)
        {
            var font = iText.Kernel.Font.PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            var gs = new PdfExtGState().SetFillOpacity(0.10f);

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                Rectangle pageSize = page.GetPageSize();

                var pdfCanvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);
                pdfCanvas.SaveState();
                pdfCanvas.SetExtGState(gs);

                using var layoutCanvas = new Canvas(page, pageSize);

                var paragraph = new Paragraph(watermarkText)
                    .SetFont(font)
                    .SetFontSize(36)
                    .SetFontColor(ColorConstants.BLACK)
                    .SetTextAlignment(TextAlignment.CENTER);

                layoutCanvas.ShowTextAligned(
                    paragraph,
                    pageSize.GetWidth() / 2f,
                    pageSize.GetHeight() / 2f,
                    i,
                    TextAlignment.CENTER,
                    VerticalAlignment.MIDDLE,
                    (float)(Math.PI / 4)
                );

                layoutCanvas.Close();
                pdfCanvas.RestoreState();
            }
        }

        private static void CleanFieldAppearance(PdfAcroForm form)
        {
            foreach (var field in form.GetAllFormFields().Values)
            {
                var widgets = field.GetWidgets();
                if (widgets == null) continue;

                foreach (var widget in widgets)
                {
                    widget.Remove(PdfName.BS);
                    widget.Put(PdfName.Border, new PdfArray(new float[] { 0, 0, 0 }));

                    var mk = widget.GetPdfObject().GetAsDictionary(PdfName.MK);
                    if (mk != null)
                    {
                        mk.Remove(PdfName.BG);
                        if (mk.KeySet().Count == 0)
                            widget.GetPdfObject().Remove(PdfName.MK);
                    }

                    widget.GetPdfObject().Remove(PdfName.AP);
                }
            }

            form.SetNeedAppearances(true);
        }

        private static (decimal pdDeath, decimal accMed, decimal premium) ComputeBenefitsFromUnits(int units)
        {
            if (units <= 0)
                return (0m, 0m, 0m);

            const decimal unitPrice = 1500m;
            var pdDeath = units * 1_000_000m;
            var accMed = units * 100_000m;
            var premium = units * unitPrice;
            return (pdDeath, accMed, premium);
        }

        private static string Naira(decimal amount) => $"₦{amount:N0}";

        public async Task SendPolicyEmail(Guid id, CancellationToken ct)
        {
            var policy = await _PolicyService.GetById(id);

            if (policy == null)
                throw new InvalidOperationException("Policy not found.");

            if (policy.ReceiptStep.Status != WorkflowStepStatus.Succeeded)
                throw new InvalidOperationException("Receipt Step not Completed.");

            var to = policy.Email;
            if (string.IsNullOrWhiteSpace(to))
                throw new InvalidOperationException("Recipient email missing.");

            var fullName = $"{policy.FirstName} {policy.LastName}".Trim();

            var now = DateTime.UtcNow;

            policy.UpdatedAt = now;
            await _context.SaveChangesAsync(ct);

            try
            {

                byte[] pdfBytes = await GenerateIndividualPolicyCertificateAsync(policy, ct);
                pdfBytes = OptimizePdf(pdfBytes);

                var logoPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "img",
                    "New Unitrust Logo Blue 1.png"
                );

                logoPath = Path.GetFullPath(logoPath);

                if (!File.Exists(logoPath))
                    throw new FileNotFoundException($"Logo not found at {logoPath}");

                var logoBytes = await File.ReadAllBytesAsync(logoPath, ct);
                var logoBase64 = Convert.ToBase64String(logoBytes);

                // 3. Email content
                var subject = $"Unitrust Policy Certificate - {policy.PolicyNumber}";

                var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;background:#f4f7fb;padding:30px'>
  <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:10px;overflow:hidden'>

    <div style='background:#0d6efd;color:#fff;padding:20px;text-align:center'>

        <img src={logoPath} />

      <h2 style='margin:0'>Unitrust Insurance</h2>
      <p style='margin:5px 0 0;font-size:13px'>Policy Certificate</p>
    </div>

    <div style='padding:25px;color:#333'>
      <p>Hello <b>{fullName}</b>,</p>

      <p>Your policy is now active.</p>

      <div style='background:#f1f6ff;border-left:4px solid #0d6efd;padding:15px;margin:20px 0'>
        <b>Policy Number:</b> {policy.PolicyNumber}
      </div>

      <p>Please find your policy certificate attached.</p>

      <p style='margin-top:30px'>
        Regards,<br/>
        <b>Unitrust Insurance</b>
      </p>
    </div>

    <div style='background:#f4f7fb;padding:20px;text-align:center;font-size:12px;color:#777'>
      📞 +234700UNITRUST | ✉ customerservice@unitrustinsurance.com | 🌐 www.unitrustinsurance.com
      <br/><br/>
      © {DateTime.UtcNow.Year} Unitrust Insurance
    </div>

  </div>
</div>";

                // 4. Send email
                await _emailSender.SendEmailWithAttachmentAsync(
                    to: to,
                    subject: subject,
                    htmlMessage: body,
                    attachmentBytes: pdfBytes,
                    attachmentFileName: $"Policy-{policy.PolicyNumber}.pdf",
                    contentType: "application/pdf"
                );

                // 5. Update success state
                var done = DateTime.UtcNow;

                policy.EmailSentAt = done;
                policy.UpdatedAt = done;

                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                policy.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                throw;
            }
        }

        //public async Task SendPolicyEmail(Guid id, CancellationToken ct)
        //{
        //    var policy = await _PolicyService.GetById(id);

        //    if (policy == null)
        //        throw new InvalidOperationException("Policy not found.");

        //    var now = DateTime.UtcNow;


        //    if (policy.ReceiptStep.Status != WorkflowStepStatus.Succeeded)
        //        throw new InvalidOperationException("Receipt Step not Completed.");

        //    var to = policy.Email;
        //    if (string.IsNullOrWhiteSpace(to))
        //        throw new InvalidOperationException("Recipient email missing.");

        //    var fullName = $"{policy.FirstName} {policy.LastName}".Trim();

        //    policy.UpdatedAt = now;
        //    await _context.SaveChangesAsync();

        //    try
        //    {
        //        byte[] pdfBytes = await GenerateIndividualPolicyCertificateAsync(policy, ct);
        //        pdfBytes = OptimizePdf(pdfBytes);

        //        var logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "New Unitrust Logo Blue 1.png");
        //        var logoBytes = await File.ReadAllBytesAsync(logoPath, ct);
        //        var logoBase64 = Convert.ToBase64String(logoBytes);

        //        var subject = $"Unitrust Policy Certificate - {policy.PolicyNumber}";
        //        var body = $@"
        //    <div style='font-family:Segoe UI,Arial,sans-serif;background:#f4f7fb;padding:30px'>
        //      <div style='max-width:600px;margin:auto;background:#ffffff;border-radius:10px;overflow:hidden'>

        //        <div style='background:#0d6efd;color:#fff;padding:20px;text-align:center'>

        //            <img src=""data:image/png;base64,{{logoBase64}}""
        //                 alt=""Unitrust Logo""
        //                 style=""height:50px;margin-bottom:10px"" />
        //          <h2 style='margin:0'>Unitrust Insurance</h2>
        //          <p style='margin:5px 0 0;font-size:13px'>Policy Certificate</p>
        //        </div>

        //        <div style='padding:25px;color:#333'>
        //          <p>Hello <b>{fullName}</b>,</p>

        //          <p>Your policy is now active.</p>

        //          <div style='background:#f1f6ff;border-left:4px solid #0d6efd;padding:15px;margin:20px 0'>
        //            <b>Policy Number:</b> {policy.PolicyNumber}
        //          </div>

        //          <p>Please find your policy certificate attached.</p>

        //          <p style='margin-top:30px'>
        //            Regards,<br/>
        //            <b>Unitrust Insurance</b>
        //          </p>
        //        </div>

        //        <div style='background:#f4f7fb;padding:20px;text-align:center;font-size:12px;color:#777'>
        //          📞 +234700UNITRUST | ✉ customerservice@unitrustinsurance.com | 🌐 www.unitrustinsurance.com
        //          <br/><br/>
        //          © {DateTime.UtcNow.Year} Unitrust Insurance
        //        </div>

        //      </div>
        //    </div>";

        //        await _emailSender.SendEmailWithAttachmentAsync(
        //            to: to,
        //            subject: subject,
        //            htmlMessage: body,
        //            attachmentBytes: pdfBytes,
        //            attachmentFileName: $"Policy-{policy.PolicyNumber}.pdf",
        //            contentType: "application/pdf"
        //        );

        //        var done = DateTime.UtcNow;

        //        policy.EmailSentAt = done;
        //        policy.UpdatedAt = done;

        //        await _context.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        var fail = DateTime.UtcNow;

        //        policy.UpdatedAt = fail;

        //        await _context.SaveChangesAsync();

        //        throw;
        //    }
        //}



        private static byte[] OptimizePdf(byte[] input)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream();

            using var reader = new PdfReader(inputStream);
            var writerProps = new WriterProperties()
                .SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);

            using var writer = new PdfWriter(outputStream, writerProps);
            using var pdfDoc = new PdfDocument(reader, writer);

            pdfDoc.Close();
            return outputStream.ToArray();
        }
    }
}
