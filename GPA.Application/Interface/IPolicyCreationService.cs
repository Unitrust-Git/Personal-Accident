using GPA.Core.Models;
using GPA.Core.Models.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface IPolicyCreationService
    {
        Task<IndividualPolicy> CreateIndividualPolicyAsync(PaymentTransaction tx, CancellationToken ct = default);
        Task<CorporatePolicy> CreateCorporatePolicyAsync(PaymentTransaction tx, CancellationToken ct = default);
    }
}
