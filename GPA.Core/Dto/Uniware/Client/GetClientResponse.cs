
using System.Text.Json.Serialization;

namespace GPA.Core.Dto.Uniware.Client
{

    public class GetClientResponse
    {
        [JsonPropertyName("result")]
        public GetClientResult Result { get; set; } = new();
    }

    public class GetClientResult
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("record")]
        public List<GetClientRecord> Record { get; set; } = new();
    }

    public class GetClientRecord
    {
        [JsonPropertyName("client_no")]
        public string ClientNo { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("policies")]
        public List<GetClientPolicy> Policies { get; set; } = new();
    }

    public class GetClientPolicy
    {
        [JsonPropertyName("clientcode")]
        public string ClientCode { get; set; } = string.Empty;

        [JsonPropertyName("polnum")]
        public string PolicyNumber { get; set; } = string.Empty;

        [JsonPropertyName("insured")]
        public string Insured { get; set; } = string.Empty;

        [JsonPropertyName("policy_class")]
        public string PolicyClass { get; set; } = string.Empty;
    }
}
