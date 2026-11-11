using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Dto.Uniware.Client
{
    public class CreateClientResponse
    {
        public CreateClientResult? result { get; set; }
    }

    public class CreateClientResult
    {
        public string? message { get; set; }
        public string? client_no { get; set; }
        public int status { get; set; }
    }
}
