using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPA.Core.Dto.Uniware.Billing
{
        public sealed class CreateReceiptRequest
        {
            [JsonPropertyName("polnum")]
            public string Polnum { get; set; } = null!;

            [JsonPropertyName("payment_ref")]
            public string PaymentRef { get; set; } = null!;

            [JsonPropertyName("transaction_date")]
            public string TransactionDate { get; set; } = null!;

            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }

            [JsonPropertyName("idocnum")]
            public string Idocnum { get; set; } = null!;

            [JsonPropertyName("bank")]
            public string Bank { get; set; } = null!;
        }

        public sealed class CreateReceiptResponse
        {
            public CreateReceiptResult? result { get; set; }
            public string? raw { get; set; }
        }

        public sealed class CreateReceiptResult
        {
            public bool status { get; set; }
            public string? refnum { get; set; }
        }

}
