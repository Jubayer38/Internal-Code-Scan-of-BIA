using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class MnpPortInCancellReqModel
    {
        public MnpPortInCancellData data { get; set; } = new MnpPortInCancellData();
    }
    public class MnpPortInCancellData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public MnpPortInCancellAttributes attributes { get; set; } = new MnpPortInCancellAttributes();
    }

    public class MnpPortInCancellAttributes
    {
        public string id { get; set; } = string.Empty;
        public string biometric_request_id { get; set; } = string.Empty;        
    }
}
