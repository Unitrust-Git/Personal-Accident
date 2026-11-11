using GPA.Core.Dto.Uniware.Billing;
using GPA.Core.Dto.Uniware.Billing.GPA.Core.Dto.Uniware.Others;
using GPA.Core.Dto.Uniware.Client;
using GPA.Core.Dto.Uniware.Others;
using GPA.Core.Dto.Uniware.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GPA.Infrastructure.HttpClients
{

    public class UniwareClient : IUniwareClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<UniwareClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public UniwareClient( HttpClient http, IConfiguration config,  ILogger<UniwareClient> logger)
        {
            _http = http;
            _logger = logger;

            var baseUrl = config["Uniware:BaseUrl"] ?? throw new Exception("Configuration missing: Uniware:BaseUrl");

            _http.BaseAddress = new Uri(baseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<CreatePolicyResponse> CreatePolicyAsync(CreatePolicyRequest request)
        {
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

            _logger.LogInformation(
                "📨 Sending General Accident policy creation request to Uniware. Request Body: {RequestBody}",
                requestJson
            );

            using var response = await _http.PostAsJsonAsync(
                "api/prod/create-policy-gpa",
                request,
                _jsonOptions);

            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Uniware policy response ({StatusCode}): {resp}",
                (int)response.StatusCode, content);

            if (!response.IsSuccessStatusCode)
                return new CreatePolicyResponse { raw = content };

            try
            {
                var result = JsonSerializer.Deserialize<CreatePolicyResponse>(content, _jsonOptions);

                if (result == null)
                    return new CreatePolicyResponse { raw = content };

                result.raw = content;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Uniware response.");
                return new CreatePolicyResponse { raw = content };
            }
        }

        public async Task<string?> SearchClientAsync(string email, string phone)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var clientNo = await SearchByQuery("email", email);
                if (clientNo != null)
                    return clientNo;
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var clientNo = await SearchByQuery("phone_no", phone);
                if (clientNo != null)
                    return clientNo;
            }

            return null;
        }

        private async Task<string?> SearchByQuery(string field, string value)
        {
            const string operation = "UniTrustClientSearch";

            var url = $"https://uniware.unitrustinsurance.com/api/prod/search-client?{field}={Uri.EscapeDataString(value)}";
            _logger.LogInformation("[{Operation}] Searching by {Field}: {Value}", operation, field, value);

            HttpResponseMessage res;

            try
            {
                res = await _http.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new HttpRequestException(
                    $"[{operation}] Network failure while calling UniTrust search endpoint.",
                    ex);
            }

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                throw new HttpRequestException(
                    $"[{operation}] UniTrust returned non-success status code {(int)res.StatusCode} ({res.StatusCode}). " +
                    $"Response: {body}");
            }

            var responseBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            SearchClientResponse? result;

            try
            {
                result = JsonSerializer.Deserialize<SearchClientResponse>(responseBody, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[{operation}] Failed to deserialize UniTrust response. Body: {responseBody}",
                    ex);
            }

            if (result?.result?.status != 1)
                return null;

            var record = result.result.record?.FirstOrDefault();
            if (record == null)
                return null;

            return record.client_no;
        }

        public async Task<string?> CreateClientAsync(CreateClientRequest request)
        {
            const string operation = "UniTrustCreateClient";
            var url = "https://uniware.unitrustinsurance.com/api/prod/create-client";

            _logger.LogInformation("[{Operation}] Creating client at UniTrust...", operation);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage res;

            try
            {
                res = await _http.PostAsync(url, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new HttpRequestException(
                    $"[{operation}] Network failure while calling UniTrust create-client endpoint.",
                    ex);
            }

            var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"[{operation}] UniTrust returned non-success status code {(int)res.StatusCode} ({res.StatusCode}). " +
                    $"Response: {body}");
            }

            CreateClientResponse? result;

            try
            {
                result = JsonSerializer.Deserialize<CreateClientResponse>(body, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[{operation}] Failed to deserialize UniTrust create-client response. Body: {body}",
                    ex);
            }

            return result?.result?.client_no;
        }

        public async Task<GetCloResponse?> GetCloAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Fetching CLO list from Uniware...");

            using var res = await _http.GetAsync("api/prod/get-clo", ct);

            var url = res.RequestMessage?.RequestUri;

            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Get-CLO response ({StatusCode}): {Body}",
                (int)res.StatusCode, body);

            if (!res.IsSuccessStatusCode)
                return null;

            try
            {

                return JsonSerializer.Deserialize<GetCloResponse>(body, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Get-CLO response.");
                return null;
            }


        }

        public async Task<GetClientResponse?> GetClientAsync(string client_no,CancellationToken ct = default)
        {
            _logger.LogInformation("Fetching client from Uniware...");

            using var res = await _http.GetAsync($"api/prod/get-client?client_no={client_no}", ct);

            var url = res.RequestMessage?.RequestUri;

            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Get-Client response ({StatusCode}): {Body}",
                (int)res.StatusCode, body);

            if (!res.IsSuccessStatusCode)
                return null;

            try
            {

                return JsonSerializer.Deserialize<GetClientResponse>(body, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Get-Client response.");
                return null;
            }


        }

        public async Task<GenerateBillingResponse> GenerateBillingAsync(string polnum, CancellationToken ct = default)
        {

            _logger.LogInformation("Generating billing on Uniware for policy {Polnum}...", polnum);

            var payload = new GenerateBillingRequest { Polnum = polnum };

            using var res = await _http.PostAsJsonAsync("api/prod/genererate-billing", payload, _jsonOptions, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Generate-billing response ({StatusCode}): {Body}",
                (int)res.StatusCode, body);

            if (!res.IsSuccessStatusCode)
                return new GenerateBillingResponse { raw = body };

            try
            {

                var result = JsonSerializer.Deserialize<GenerateBillingResponse>(body, _jsonOptions)
                             ?? new GenerateBillingResponse();

                result.raw = body;
                return result;

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Failed to parse generate-billing response.");
                return new GenerateBillingResponse { raw = body };

            }
        }

        public async Task<CreateReceiptResponse> CreateReceiptAsync(CreateReceiptRequest request, CancellationToken ct = default)
        {
            _logger.LogInformation("Creating receipt on Uniware for policy {Polnum} (idocnum={Idocnum})...",
                request.Polnum, request.Idocnum);

            using var res = await _http.PostAsJsonAsync("api/prod/create-receipt", request, _jsonOptions, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("Create-receipt response ({StatusCode}): {Body}",
                (int)res.StatusCode, body);

            if (!res.IsSuccessStatusCode)
                return new CreateReceiptResponse { raw = body };

            try
            {
                var parsed = JsonSerializer.Deserialize<CreateReceiptResponse>(body, _jsonOptions)
                             ?? new CreateReceiptResponse();

                parsed.raw = body;
                return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse create-receipt response.");
                return new CreateReceiptResponse { raw = body };
            }
        }

        public async Task<GetPolicyDetailsResponse?> GetPolicyDetailsAsync(string polnum, CancellationToken ct = default)
        {
            const string operation = "UniwareGetPolicyDetails";

            if (string.IsNullOrWhiteSpace(polnum))
                throw new ArgumentException("Policy number is required.", nameof(polnum));

            var url = $"api/prod/get-policy-details?polnum={Uri.EscapeDataString(polnum)}";

            _logger.LogInformation("[{Operation}] Fetching policy details for policy number {Polnum}...", operation, polnum);

            using var res = await _http.GetAsync(url, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("[{Operation}] Response ({StatusCode}): {Body}",
                operation, (int)res.StatusCode, body);

            if (!res.IsSuccessStatusCode)
                return new GetPolicyDetailsResponse { raw = body };

            try
            {
                var result = JsonSerializer.Deserialize<GetPolicyDetailsResponse>(body, _jsonOptions)
                             ?? new GetPolicyDetailsResponse();

                result.raw = body;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Operation}] Failed to parse get-policy-details response.", operation);
                return new GetPolicyDetailsResponse { raw = body };
            }
        }

        public async Task<SearchClientResponse?> FullSearchClientAsync(string email, string phone)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {

                var nphone = NormalizePhoneNumber(phone);

                var response = await SearchClientsByQuery("phone_no", nphone);
                if (response != null)
                    return response;

                var response2 = await SearchClientsByQuery("phone_no", phone);
                if (response2 != null)
                    return response2;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var response = await SearchClientsByQuery("email", email);
                if (response != null)
                    return response;
            }

            return null;

        }


        private static string NormalizePhoneNumber(string phone)
        {
            phone = phone.Trim();

            if (phone.StartsWith("+234"))
                return phone.Substring(1);

            if (phone.StartsWith("234"))
                return phone;

            if (phone.StartsWith("0"))
                return "234" + phone.Substring(1);

            return "234" + phone;
        }


        private async Task<SearchClientResponse?> SearchClientsByQuery(string field, string value)
        {
            const string operation = "UniTrustClientSearch";

            var url = $"https://uniware.unitrustinsurance.com/api/prod/search-client?{field}={Uri.EscapeDataString(value)}";

            _logger.LogInformation(
                "[{Operation}] Searching clients by {Field}: {Value}",
                operation,
                field,
                value);

            HttpResponseMessage res;

            try
            {
                res = await _http.GetAsync(url).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new HttpRequestException(
                    $"[{operation}] Network failure while calling UniTrust search endpoint.",
                    ex);
            }

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

                throw new HttpRequestException(
                    $"[{operation}] UniTrust returned non-success status code {(int)res.StatusCode} ({res.StatusCode}). " +
                    $"Response: {body}");
            }

            var responseBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false);

            SearchClientResponse? result;

            try
            {
                result = JsonSerializer.Deserialize<SearchClientResponse>(responseBody, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[{operation}] Failed to deserialize UniTrust response. Body: {responseBody}",
                    ex);
            }

            if (result?.result?.status != 1)
                return null;

            return result;
        }
    }
}
