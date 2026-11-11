using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPA.Core.Dto.Uniware.Client
{
    public class SearchClientResponse
    {
        public SearchClientResult result { get; set; } = new SearchClientResult();
    }

    public class SearchClientResult
    {
        public int status { get; set; }
        public string error { get; set; } = string.Empty;
        public List<SearchClientRecord> record { get; set; } = new List<SearchClientRecord>();
    }

    public class SearchClientRecord
    {
        public string client_no { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
    }
}
