
using GPA.Core.Execeptions;
using GPA.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace GPA.Infrastructure.HttpClients
{
    public class PaystackClient : IPaystackClient
    {
        private readonly HttpClient _http;
        private readonly string _appBaseUrl;


        public PaystackClient(HttpClient http, IConfiguration config)
        {
            _http = http;

            _appBaseUrl = config["App:BaseUrl"]
        ?? throw new InvalidOperationException("Missing configuration: App:BaseUrl");
        }



        public async Task<PaystackInitResponse>  Initialize(
          PaymentTransaction tx,
          CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tx.Metadata))
                throw new PaymentValidationException("Missing metadata");

            PaymentMetadata metadata;
            try
            {
                metadata = JsonSerializer.Deserialize<PaymentMetadata>(
                    tx.Metadata,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                throw new PaymentValidationException("Invalid payment metadata", ex);
            }

            if (metadata.PaymentType == "corporate" &&
                (metadata.Beneficiaries == null || !metadata.Beneficiaries.Any()))
            {
                throw new PaymentValidationException(
                    "Corporate payment must include beneficiaries");
            }

            var callbackUrl = $"{_appBaseUrl}/api/payment/callback";

            var payload = new
            {
                email = metadata.Email,
                amount = tx.AmountInKobo,
                reference = tx.Reference,
                callback_url = callbackUrl,
                metadata = JsonSerializer.Deserialize<JsonElement>(tx.Metadata)
            };


            var res = await _http.PostAsJsonAsync(
                "transaction/initialize",
                payload,
                ct);

            if (!res.IsSuccessStatusCode)
            {
                var error = await res.Content.ReadAsStringAsync(ct);
                throw new PaymentGatewayException(
                    $"Paystack initialization failed: {error}");
            }

            return await res.Content.ReadFromJsonAsync<PaystackInitResponse>(ct)
                ?? throw new PaymentGatewayException("Empty Paystack response");
        }


        public Task<string> GetAuthorizationUrl(PaymentTransaction tx, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(tx.GatewayReference))
                throw new InvalidOperationException("No GatewayReference found for this transaction.");

            return Task.FromResult($"https://checkout.paystack.com/{tx.AccessCode}");
        }

    }
}
