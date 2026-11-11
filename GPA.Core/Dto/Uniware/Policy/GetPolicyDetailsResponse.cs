using System.Text.Json.Serialization;

namespace GPA.Core.Dto.Uniware.Policy
{
    public class GetPolicyDetailsResponse
    {
        public List<GetPolicyDetailsResult>? result { get; set; }

        [JsonIgnore]
        public string? raw { get; set; }
    }

    public class GetPolicyDetailsResult
    {
        public string? policy_no { get; set; }
        public string? insured { get; set; }
        public string? policy_class { get; set; }
        public string? risktype { get; set; }
        public string? agency { get; set; }
        public string? bsntype { get; set; }
        public string? issuedate { get; set; }
        public int? period_term { get; set; }
        public string? effective_date { get; set; }
        public string? end_date { get; set; }
        public int? policy_status { get; set; }
        public decimal? sum_assured { get; set; }
        public decimal? premium { get; set; }
        public string? naicom_uid { get; set; }
        public string? clientcode { get; set; }
        public List<GetPolicyDetailsItem>? items { get; set; }
        public List<GetPolicyDetailsDebitNote>? debit_notes { get; set; }
    }

    public class GetPolicyDetailsItem
    {
        public string? itemcode { get; set; }
        public string? covertype { get; set; }
        public string? sum_assured { get; set; }
        public string? premium { get; set; }
        public string? item_strdate { get; set; }
        public string? item_enddate { get; set; }
        public string? cert_no { get; set; }
        public string? registration_no { get; set; }
        public string? vehicle_make { get; set; }
        public string? vehicle_make_code { get; set; }
        public string? vehicle_type { get; set; }
        public string? vehicle_type_code { get; set; }
        public string? vehicle_model { get; set; }
        public string? vehicle_model_code { get; set; }
        public string? chasis_no { get; set; }
        public string? engine_no { get; set; }
        public string? engine_capacity { get; set; }
        public string? vehicle_year { get; set; }
        public string? vehicle_color { get; set; }
    }

    public class GetPolicyDetailsDebitNote
    {
        public string? debitnote_no { get; set; }
        public decimal? amount { get; set; }
        public string? period { get; set; }
        public string? effective_date { get; set; }
    }
}