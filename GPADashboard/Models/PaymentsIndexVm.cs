namespace GPADashboard.Models
{
    using GPA.Core.Enums;
    using GPA.Application.DTO;

    public class PaymentsIndexVm
    {
        public IReadOnlyList<PaymentTransactionDto> Items { get; set; } = [];
        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public PaymentStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
    }

}
