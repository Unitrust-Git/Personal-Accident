using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Execeptions
{
    public class PaymentValidationException : Exception
    {
        public PaymentValidationException(string message, Exception? inner = null)
            : base(message, inner) { }
    }

    public class PaymentGatewayException : Exception
    {
        public PaymentGatewayException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
