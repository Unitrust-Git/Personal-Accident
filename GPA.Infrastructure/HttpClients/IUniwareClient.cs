using GPA.Core.Dto.Uniware.Billing;
using GPA.Core.Dto.Uniware.Billing.GPA.Core.Dto.Uniware.Others;
using GPA.Core.Dto.Uniware.Client;
using GPA.Core.Dto.Uniware.Others;
using GPA.Core.Dto.Uniware.Policy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Infrastructure.HttpClients
{
    public interface IUniwareClient
    {

        Task<CreatePolicyResponse> CreatePolicyAsync(CreatePolicyRequest request);
        Task<string?> CreateClientAsync(CreateClientRequest request);
        Task<string?> SearchClientAsync(string email, string phone);
        Task<GetClientResponse?> GetClientAsync(string client_no, CancellationToken ct = default);
        Task<GetCloResponse?> GetCloAsync(CancellationToken ct = default);
        Task<GenerateBillingResponse> GenerateBillingAsync(string polnum, CancellationToken ct = default);
        Task<CreateReceiptResponse> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken ct = default);
        Task<GetPolicyDetailsResponse?> GetPolicyDetailsAsync(string polnum, CancellationToken ct = default);

        Task<SearchClientResponse?> FullSearchClientAsync(string email, string phone);

    }
}
