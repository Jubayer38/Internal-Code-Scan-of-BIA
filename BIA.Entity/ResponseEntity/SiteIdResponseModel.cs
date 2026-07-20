using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class SiteIdResponseModel
    {
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
        public BTSCode data { get; set; } = new BTSCode();
    }
    public class BTSCode
    {
        public string bts_code { get; set; } = string.Empty;
    }
}
