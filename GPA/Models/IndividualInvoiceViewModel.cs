namespace GPA.Models
{
    public class IndividualInvoiceViewModel
    {
        public string InvoiceId { get; set; }

        // Client reference
        public string ClientCode { get; set; }


        // Display only
        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string NextOfKinName { get; set; }

        public string NextOfKinPhone { get; set; }

        public int Units { get; set; }

        public decimal UnitPrice { get; set; }


        public bool IsReferral { get; set; }

        public string CloCode { get; set; }

        public string CloName { get; set; }
    }
}
