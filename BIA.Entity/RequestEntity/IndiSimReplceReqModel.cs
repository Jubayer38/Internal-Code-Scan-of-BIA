using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class IndiSimReplceReqModel
    {
        public IndiSimReplceData data { get; set; } = new IndiSimReplceData();
    }

    public class IndiSimReplceData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public IndiSimReplceAttributes attributes { get; set; } = new IndiSimReplceAttributes();
    }

    public class IndiSimReplceAttributes
    {
        [JsonProperty(PropertyName = "biometric-request-id")]
        public string biometric_request_id { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "new-icc")]
        public string new_icc { get; set; } = string.Empty;
        public string reason { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "payment-mode")]
        public string payment_mode { get; set; } = string.Empty;
        public IndiSimReplceMeta meta { get; set; } = new IndiSimReplceMeta();
    }

    public class IndiSimReplceMeta
    {
        public string channel { get; set; } = string.Empty;
        public string reseller { get; set; } = string.Empty;
        public string salesman { get; set; } = string.Empty;        
    }
}
