using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class SIMProductMapResponse
    {
        public bool is_success { get; set; }
        public string message { get; set; } = string.Empty;
        public string product_code { get; set; } = string.Empty;
    }
}
