using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMErrorMsg
    {
        public string error_msg { get; set; } = string.Empty;
        public string error_code { get; set; } = string.Empty;
    }

    public class VMErrorId
    {
        public long error_id { get; set; } = 0;
    }
}
