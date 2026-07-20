using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class QCStatusResponseRootobject
    {
        public List<QCStatusResponseDatum> data { get; set; } = new List<QCStatusResponseDatum>();
    }

    public class QCStatusResponseDatum
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public QCStatusResponseAttributes attributes { get; set; } = new QCStatusResponseAttributes();
    }

    public class QCStatusResponseAttributes
    {
        public string requestid { get; set; } = string.Empty;
        public string href { get; set; } = string.Empty;
        public DateTime scheduledat { get; set; }
        public string status { get; set; } = string.Empty;      
    }
}
