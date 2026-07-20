using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class ICCDetailsResponse
    {
        public string product_name { get; set; } = string.Empty;
        public string offer_name { get; set; } = string.Empty;
        public string offer_description { get; set; } = string.Empty;
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
 