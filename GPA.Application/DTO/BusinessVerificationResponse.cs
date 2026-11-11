using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GPA.Application.DTO
{
    public class BusinessVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public BusinessData Data { get; set; } = new BusinessData();

        [JsonPropertyName("links")]
        public List<object>? Links { get; set; }

        [JsonIgnore]
        public bool IsFound => Data?.Status == "found";
    }

    public class BusinessData
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("registrationNumber")]
        public string? RegistrationNumber { get; set; }

        [JsonPropertyName("registryNumber")]
        public string? RegistryNumber { get; set; }

        [JsonPropertyName("typeOfEntity")]
        public string? TypeOfEntity { get; set; }

        [JsonPropertyName("activity")]
        public string? Activity { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("branchAddress")]
        public string? BranchAddress { get; set; }

        [JsonPropertyName("companyStatus")]
        public string? CompanyStatus { get; set; }

        [JsonPropertyName("isConsent")]
        public bool IsConsent { get; set; }

        [JsonPropertyName("companyContactPersons")]
        public List<CompanyContactPerson> CompanyContactPersons { get; set; } = new List<CompanyContactPerson>();

        [JsonPropertyName("keyPersonnel")]
        public List<KeyPersonnel> KeyPersonnel { get; set; } = new List<KeyPersonnel>();

    }

    public class CompanyContactPerson
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("contacts")]
        public ContactDetails Contacts { get; set; } = new ContactDetails();
    }

    public class ContactDetails
    {
        [JsonPropertyName("email")]
        public List<string> Email { get; set; } = new List<string>();

        [JsonPropertyName("phone")]
        public List<string> Phone { get; set; } = new List<string>();
    }

    public class KeyPersonnel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("designation")]
        public string? Designation { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

    }
}
