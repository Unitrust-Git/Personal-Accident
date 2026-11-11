using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = false;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "Unitrust";
    }

}
