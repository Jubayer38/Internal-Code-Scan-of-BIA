using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class SiteIdResponseModelV2
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public BTSCodeV2 data { get; set; } = new BTSCodeV2();
    }
    public class BTSCodeV2
    {
        public string bts_code { get; set; } = string.Empty;
        public bool is_lus { get; set; } = false;
        public bool is_cherish { get; set; }
    }
}
