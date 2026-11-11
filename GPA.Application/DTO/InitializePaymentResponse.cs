using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class InitializePaymentResponse
    {
        public string AuthorizationUrl { get; set; } = default!;
        public string Reference { get; set; } = default!;
    }

}
