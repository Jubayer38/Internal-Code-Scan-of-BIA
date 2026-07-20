using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class DMSICCCheckRequest
    {
        public string retailerCode { get; set; } = string.Empty;
        public string serialNo { get; set; } = string.Empty;
    }
}
