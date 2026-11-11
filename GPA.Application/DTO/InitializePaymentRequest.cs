using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class InitializePaymentRequest
    {

        public decimal Amount { get; set; }
        public string? Metadata { get; set; }

    }

}
