using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class CustomerUpdateRespRootobject
    {
        public List<CustomerUpdateRespDatum> data { get; set; } = new List<CustomerUpdateRespDatum>();
    }

    public class CustomerUpdateRespDatum
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public CustomerUpdateRespAttributes attributes { get; set; } = new CustomerUpdateRespAttributes();
    }

    public class CustomerUpdateRespAttributes
    {
        public string requestid { get; set; } = string.Empty;
        public string href { get; set; } = string.Empty;
        public DateTime scheduledat { get; set; }
        public string status { get; set; } = string.Empty;
    }
}
