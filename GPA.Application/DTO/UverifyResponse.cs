using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class UverifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public UverifyData Data { get; set; }

        [JsonPropertyName("links")]
        public List<object>? Links { get; set; }
    }

    public class UverifyData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("address")]
        public Address? Address { get; set; }

        [JsonPropertyName("parentId")]
        public string? ParentId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("dataValidation")]
        public bool DataValidation { get; set; }

        [JsonPropertyName("selfieValidation")]
        public bool SelfieValidation { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("signature")]
        public string? Signature { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("birthState")]
        public string? BirthState { get; set; }

        [JsonPropertyName("nokState")]
        public string? NokState { get; set; }

        [JsonPropertyName("religion")]
        public string? Religion { get; set; }

        [JsonPropertyName("birthLGA")]
        public string? BirthLGA { get; set; }

        [JsonPropertyName("birthCountry")]
        public string? BirthCountry { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [JsonPropertyName("isConsent")]
        public bool IsConsent { get; set; }

        [JsonPropertyName("idNumber")]
        public string IdNumber { get; set; }

        [JsonPropertyName("businessId")]
        public string? BusinessId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("allValidationPassed")]
        public bool AllValidationPassed { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("requestedAt")]
        public DateTime? RequestedAt { get; set; }

        [JsonPropertyName("requestedById")]
        public string? RequestedById { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("lastModifiedAt")]
        public DateTime? LastModifiedAt { get; set; }

        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }

        [JsonPropertyName("requestedBy")]
        public RequestedBy? RequestedBy { get; set; }
    }

    public class Address
    {
        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("lga")]
        public string? Lga { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("addressLine")]
        public string? AddressLine { get; set; }
    }

    public class RequestedBy
    {
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
