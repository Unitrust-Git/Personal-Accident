using GPA.Core.Models.Policy;

namespace GPADashboard.Models
{
    public class IndividualPoliciesIndexVm
    {
        public List<IndividualPolicy> Items { get; set; } = new();
        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }

        public string? Search { get; set; }
        public string? PolicyNumber { get; set; }
        public Guid? PaymentTransactionId { get; set; }

        public List<PolicyReconciliationResult> Reconciliation { get; set; }
    = new();
    }
}
