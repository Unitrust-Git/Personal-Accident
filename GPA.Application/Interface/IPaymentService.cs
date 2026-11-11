using GPA.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.Interface
{
    public interface IPaymentService
    {
        Task<InitializePaymentResponse> InitializeAsync(
                    InitializePaymentRequest request, string invoiceId,
                    CancellationToken ct);

        Task<PaymentProcessingResult> ProcessPaymentSuccessAsync(PaystackPaymentData data, CancellationToken ct = default);
        Task<PaymentProcessingResult> ProcessPaymentFailureAsync(PaystackPaymentData data);

        Task<string> HandlePaymentCallbackAsync(string reference, CancellationToken ct = default);
    }
}
