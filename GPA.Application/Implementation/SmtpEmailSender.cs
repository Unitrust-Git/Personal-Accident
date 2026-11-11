
using GPA.Application.Interface;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace GPA.Application.Implementation
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public Task SendEmailAsync(string to, string subject, string htmlMessage)
            => SendInternalAsync(to, subject, htmlMessage, null, null, null);

        public Task SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string htmlMessage,
            byte[] attachmentBytes,
            string attachmentFileName,
            string contentType = "application/pdf")
        {
            if (attachmentBytes == null || attachmentBytes.Length == 0)
                throw new ArgumentException("Attachment is empty.", nameof(attachmentBytes));

            return SendInternalAsync(
                to,
                subject,
                htmlMessage,
                attachmentBytes,
                attachmentFileName,
                contentType);
        }

        private async Task SendInternalAsync(
            string to,
            string subject,
            string htmlMessage,
            byte[]? attachmentBytes,
            string? attachmentFileName,
            string? contentType)
        {
            var smtpSection = _config.GetSection("Smtp");

            var host = smtpSection["Host"];
            var portString = smtpSection["Port"];
            var username = smtpSection["Username"];
            var password = smtpSection["SmtpPassword"];
            var from = smtpSection["FromAddress"];

            // Optional limits (recommended)
            // Put this in appsettings: "Smtp": { "MaxAttachmentBytes": 8388608 }
            var maxAttachmentBytesStr = smtpSection["MaxAttachmentBytes"];
            var maxAttachmentBytes =
                int.TryParse(maxAttachmentBytesStr, out var parsed) && parsed > 0
                    ? parsed
                    : 8 * 1024 * 1024; // default 8MB safe cap for O365 (base64 overhead)

            try
            {
                if (string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(portString) ||
                    string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(from))
                {
                    throw new InvalidOperationException("SMTP configuration is incomplete.");
                }

                if (!int.TryParse(portString, out var port))
                    throw new InvalidOperationException("SMTP port is invalid.");

                // --- Pre-check attachment size to avoid 5.3.4 ---
                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    var bytes = attachmentBytes.Length;
                    var mb = bytes / 1024d / 1024d;

                    // Rough base64 overhead estimate (~33%)
                    var estimatedEncodedBytes = (long)Math.Ceiling(bytes * 4d / 3d);
                    var encodedMb = estimatedEncodedBytes / 1024d / 1024d;

                    Console.WriteLine($"[SMTP] Attachment: {attachmentFileName ?? "attachment"} " +
                                      $"raw={mb:0.00}MB ({bytes} bytes), " +
                                      $"est_base64={encodedMb:0.00}MB");

                    if (bytes > maxAttachmentBytes)
                    {
                        var maxMb = maxAttachmentBytes / 1024d / 1024d;
                        throw new InvalidOperationException(
                            $"Attachment too large to send via SMTP. " +
                            $"Raw={mb:0.00}MB exceeds limit={maxMb:0.00}MB. " +
                            $"(Office 365 counts base64/MIME overhead too.)");
                    }
                }

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage(from, to)
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                // Attach PDF if provided
                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    // IMPORTANT: let Attachment own/dispose the stream via mailMessage.Dispose()
                    var stream = new MemoryStream(attachmentBytes, writable: false);
                    var attachment = new Attachment(
                        stream,
                        attachmentFileName ?? "attachment.pdf",
                        contentType ?? "application/pdf");

                    mailMessage.Attachments.Add(attachment);
                }

                await client.SendMailAsync(mailMessage);
            }
            catch (SmtpException smtpEx)
            {
                // This will include the 5.3.4 error message but not the actual size/limit.
                Console.Error.WriteLine($"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to send email: {ex.Message}");
                throw;
            }
        }
    }
}
