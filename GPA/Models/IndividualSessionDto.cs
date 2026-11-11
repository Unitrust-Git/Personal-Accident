namespace GPA.Models
{
    public class IndividualSessionDto
    {

        public string ClientCode { get; set; }


        public string FullName { get; set; }


        public string Email { get; set; }


        public string Phone { get; set; }


        public string NextOfKinName { get; set; }


        public string NextOfKinPhone { get; set; }



        public int Units { get; set; }



        public bool IsReferral { get; set; }


        public string CloCode { get; set; }


        public string CloName { get; set; }

    }

    public class AddressDto
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Town { get; set; } = "";           // ✅ added
        public string Country { get; set; } = "";
    }
}
