using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class BioCancellReqModel
    {
        public BioCancellData data { get; set; } = new BioCancellData();
        public BioCancellMeta meta { get; set; } = new BioCancellMeta();
    }

    public class BioCancellData
    {
        public string type { get; set; } = "";
        public string id { get; set; } = "";
        public BioCancellAttributes attributes { get; set; } = new BioCancellAttributes();
    }

    public class BioCancellAttributes
    {
        public string biometric_request_id { get; set; } = string.Empty;
        public int status { get; set; }=0;
    }

    public class BioCancellMeta
    {
        public string reason { get; set; } = string.Empty;
        public string channel { get; set; } = string.Empty;
        public string reseller { get; set; } = string.Empty;
        public string salesman { get; set; } = string.Empty;
    }
}
