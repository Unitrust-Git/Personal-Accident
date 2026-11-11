using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GPA.Models
{
    public class CorporateInvoiceViewModel
    {
        public string InvoiceId { get; set; }
        public string Name { get; set; }
        public string RegistrationNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public List<CorporateInsuredViewModel> Beneficiaries { get; set; }
        public int UnitPrice { get; set; } = 1500;
    }


    public class CorporateInsuredViewModel
    {
        [JsonProperty("name")]
        public string BenName { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("units")]
        public int Units { get; set; }
    }


}
