using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Application.DTO
{
    public class SearchClientResponse
    {
        public SearchResult result { get; set; }
    }

    public class SearchResult
    {
        public int status { get; set; }

        public List<ClientRecord> record { get; set; }
    }

    public class ClientRecord
    {
        public string client_no { get; set; }

        public string first_name { get; set; }

        public string surname { get; set; }

        public string phone_no { get; set; }
    }
}
