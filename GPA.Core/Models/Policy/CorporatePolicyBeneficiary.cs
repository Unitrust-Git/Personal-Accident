using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Models.Policy
{
        public class CorporatePolicyBeneficiary
        {
            public Guid Id { get; set; }

            public Guid CorporatePolicyId { get; set; }
            public CorporatePolicy CorporatePolicy { get; set; } = default!;

            public string FullName { get; set; } = default!;

            public string PhoneNumber { get; set; } = default!;
            public int units { get; set; }


        }
}
