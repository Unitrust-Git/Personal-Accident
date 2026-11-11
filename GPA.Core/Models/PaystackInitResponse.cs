using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace GPA.Core.Models
{
    public class PaystackInitResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;

        [JsonPropertyName("data")]
        public PaystackInitData Data { get; set; } = default!;
    }

    public class PaystackInitData
    {
        [JsonPropertyName("authorization_url")]
        public string AuthorizationUrl { get; set; } = default!;

        [JsonPropertyName("access_code")]
        public string AccessCode { get; set; } = default!;

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = default!;
    }
}

