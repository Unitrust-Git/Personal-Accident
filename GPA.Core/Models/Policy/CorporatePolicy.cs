using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Models.Policy
{
    public class CorporatePolicy : PolicyBase
    {
        public string CompanyName { get; set; } = default!;
        public string CompanyEmail { get; set; } = default!;
        public string CompanyAddress { get; set; } = default!;

        public int TotalEmployees { get; set; }
        public decimal TotalSumInsured { get; set; }

        public Guid PaymentTransactionId { get; set; }

        public PaymentTransaction? PaymentTransaction { get; set; }

        public ICollection<CorporatePolicyBeneficiary> Beneficiaries { get; set; }
            = new List<CorporatePolicyBeneficiary>();

    }

}
