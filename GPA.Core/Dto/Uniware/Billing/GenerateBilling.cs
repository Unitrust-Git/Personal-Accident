using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Dto.Uniware.Billing
{
    using System.Text.Json.Serialization;

    namespace GPA.Core.Dto.Uniware.Others
    {
        public sealed class GenerateBillingRequest
        {
            [JsonPropertyName("polnum")]
            public string Polnum { get; set; } = null!;
        }

        public sealed class GenerateBillingResponse
        {
            public GenerateBillingResult? result { get; set; }

            public string? raw { get; set; }
        }

        public sealed class GenerateBillingResult
        {
            public int status { get; set; }
            public string? dcnumb { get; set; }
            public string? naicom_id { get; set; }
            public string? message { get; set; }
        }
    }

}
