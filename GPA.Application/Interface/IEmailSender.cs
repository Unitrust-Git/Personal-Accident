using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);

        Task SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string htmlMessage,
            byte[] attachmentBytes,
            string attachmentFileName,
            string contentType = "application/pdf");
    }


}
