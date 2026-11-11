namespace GPA.Models
{
    public class IndividualInvoiceRequest
    {
        public string ClientCode { get; set; }


        // Display only
        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }


        // Coverage
        public int Units { get; set; }


        // Next of kin
        public string NextOfKinName { get; set; }

        public string NextOfKinPhone { get; set; }


        // Referral
        public bool IsReferral { get; set; }

        public string CloCode { get; set; }

        public string CloName { get; set; }
    }
}
