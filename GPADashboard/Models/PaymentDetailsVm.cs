namespace GPADashboard.Models
{
    using GPA.Application.DTO;

    public class PaymentDetailsVm
    {
        public PaymentTransactionDto Payment { get; set; } = default!;
    }
}
