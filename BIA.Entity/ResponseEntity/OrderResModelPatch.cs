using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class OrderResModelPatch
    {
        public List<OrderResPathDatum> data { get; set; } = new List<OrderResPathDatum>();
    }

    public class OrderResPathDatum
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public OrderResPathAttributes attributes { get; set; } = new OrderResPathAttributes();
    }

    public class OrderResPathAttributes
    {
        public string requestid { get; set; } = string.Empty;
        public string href { get; set; } = string.Empty;
        public DateTime scheduledat { get; set; }
        public string status { get; set; } = string.Empty;      
    }
}
