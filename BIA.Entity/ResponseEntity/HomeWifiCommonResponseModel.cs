using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class HomeWifiCommonResponseModel
    {
        public bool isError { get; set; }

        public string? message { get; set; }

        public object? data { get; set; }
    }

    public class HomeWifiLeadDetailsResponseData
    {
        public object lead_details { get; set; }
        public List<object> pages { get; set; }
        public object required_pages { get; set; }
    }

}
