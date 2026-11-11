using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GPA.Core.Dto.Uniware.Others
{
    public sealed class GetCloResponse
    {
        [JsonPropertyName("CLO_LIST")]
        public List<CloItem> CLO_LIST { get; set; } = new();
    }

    public sealed class CloItem
    {
        public string? name { get; set; }
        public string? code { get; set; }
    }
}
