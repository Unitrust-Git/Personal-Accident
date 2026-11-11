namespace GPA.Core.Dto.Uniware.Policy
{
    public class CreatePolicyRequest
    {
        public PolicyDetails policy_details { get; set; } = new();
        public List<ItemDetails> item_details { get; set; } = new();
    }

    public class PolicyDetails
    {
        public string? polnum { get; set; }          
        public string client_code { get; set; } = "";
        public string branch { get; set; } = "";       
        public string polagency { get; set; } = "";
        public string polclass { get; set; } = "";
        public string risktype { get; set; } = "";
        public string bsntype { get; set; } = "";
        public string issuedate { get; set; } = ""; 
        public string efcdate { get; set; } = ""; 
        public string clo { get; set; } = ""; 
        public string sbu { get; set; } = "";  
        public string business_location { get; set; } = "";
    }

    public class ItemDetails
    {
        public string itemdesc { get; set; } = ""; 
        public string risktype { get; set; } = ""; 
        public string cover_type { get; set; } = ""; 
        public string entdate { get; set; } = ""; 
        public string bsntype { get; set; } = ""; 
        public decimal insuredval { get; set; }
        public decimal desiredprem { get; set; }
        public string item_strdate { get; set; } = "";  
        public string item_enddate { get; set; } = ""; 
    }
}
