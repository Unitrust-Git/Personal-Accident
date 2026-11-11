using GPA.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.HttpClients
{
    public interface IPaystackClient
    {
        Task<PaystackInitResponse> Initialize(PaymentTransaction tx, CancellationToken ct = default);
        Task<string> GetAuthorizationUrl(PaymentTransaction tx, CancellationToken ct = default);

    }
}
