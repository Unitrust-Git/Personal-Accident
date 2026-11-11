namespace GPADashboard.Models
{
    public class PolicyReconciliationResult
    {
        public string? PolicyNumber { get; set; }

        public decimal Units { get; set; }

        public decimal ExpectedSumAssured { get; set; }

        public decimal? ActualSumAssured { get; set; }


        public string Status { get; set; } = string.Empty;

        public string? InsuredName { get; set; }

        public string? ClientCode { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? Remark { get; set; }
    }
}
