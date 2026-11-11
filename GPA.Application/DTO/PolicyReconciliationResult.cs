using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
        public class PolicyReconciliationResult
        {
            public string? PolicyNumber { get; set; }

            public decimal Units { get; set; }

            public decimal ExpectedSumAssured { get; set; }

            public decimal ActualSumAssured { get; set; }

            // VALID | MISMATCH | NOT_FOUND_IN_UNIWARE
            public string Status { get; set; } = string.Empty;

            public string? InsuredName { get; set; }

            public string? ClientCode { get; set; }

            public DateTime? EffectiveDate { get; set; }

            public DateTime? EndDate { get; set; }

            public string? Remark { get; set; }
        }
}
