using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPA.Core.Models.Policy
{
    public class IndividualPolicy : PolicyBase
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string NIN { get; set; } = default!;
        public string Email { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public int Units { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SumInsured { get; set; }
        public string? NextOfKinName { get; set; }
        public string? NextOfKinPhone { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
