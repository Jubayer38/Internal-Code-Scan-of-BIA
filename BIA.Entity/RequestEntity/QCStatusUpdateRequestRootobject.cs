using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class QCStatusUpdateRequestRootobject
    {
        public QCStatusUpdateRequestData data { get; set; } = new QCStatusUpdateRequestData();
    }

    public class QCStatusUpdateRequestData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public QCStatusUpdateRequestAttributes attributes { get; set; } = new QCStatusUpdateRequestAttributes();
    }

    public class QCStatusUpdateRequestAttributes
    {
        public string status { get; set; } = string.Empty;
        public string reseller { get; set; } = string.Empty;        
    }
}
