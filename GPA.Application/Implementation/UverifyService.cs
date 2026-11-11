using GPA.Application.DTO;
using GPA.Application.Interface;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace GPA.Application.Implementation
{
    public class UverifyService : IUverifyService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "";
        private readonly IConfiguration _config;

        public UverifyService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            var baseUrl = _config["UVerify:BaseUrl"] ?? "";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<UverifyResponse?> GetUserByNINAsync(string nin)
        {
            try
            {

                var apiKey = _config["UVerify:ApiKey"];

                var request = new HttpRequestMessage(HttpMethod.Post, "v2/api/identity/ng/nin");
                request.Headers.Add("token", apiKey);
                request.Headers.Add("Accept", "application/json"); 

                var body = new
                {
                    id = nin,
                    isSubjectConsent = true
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Uverify API returned: {response.StatusCode} | {error}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<UverifyResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error calling Uverify API: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing Uverify API response: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error calling Uverify API: {ex.Message}");
                return null;
            }
        }

        public async Task<BusinessVerificationResponse> VerifyBusinessAsync(string registrationNumber,string countryCode = "NG", string? registrationName = null,bool premium = false)
        {
            var responseDto = new BusinessVerificationResponse();

            try
            {
                var apiKey = _config["UVerify:ApiKey"];

                var request = new HttpRequestMessage(HttpMethod.Post, "v2/api/verifications/global/company-advance-check");
                request.Headers.Add("token", apiKey);
                request.Headers.Add("Accept", "application/json");

                var body = new
                {
                    registrationNumber,
                    countryCode,
                    premium,
                    isConsent = true
                };

                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    responseDto.Success = false;
                    responseDto.StatusCode = (int)response.StatusCode;
                    responseDto.Message = content;
                    responseDto.Data.Status = "not_found";
                    return responseDto;
                }

                responseDto = JsonSerializer.Deserialize<BusinessVerificationResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new BusinessVerificationResponse();

                if (responseDto.Data?.Status != "found")
                {
                    responseDto.Data.Status = "not_found";
                }

                return responseDto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying business: {ex.Message}");
                responseDto.Success = false;
                responseDto.StatusCode = 500;
                responseDto.Message = ex.Message;
                responseDto.Data.Status = "not_found";
                return responseDto;
            }
        }

    }
}
