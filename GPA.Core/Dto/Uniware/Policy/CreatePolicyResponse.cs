using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPA.Core.Dto.Uniware.Policy
{
    public class CreatePolicyResponse
    {
        [JsonPropertyName("result")]
        public CreatePolicyResult? Result { get; set; }

        [JsonIgnore]
        public bool success => Result?.Status == 1;

        [JsonIgnore]
        public string? message => Result?.Message;

        public string? raw { get; set; }
    }

    public class CreatePolicyResult
    {
        [JsonPropertyName("polnum")]
        public string PolicyNumber { get; set; } = default!;

        [JsonPropertyName("itemcode")]
        public string ItemCode { get; set; } = default!;

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;
    }

}
