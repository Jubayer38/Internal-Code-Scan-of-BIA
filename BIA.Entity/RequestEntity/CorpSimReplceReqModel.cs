using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class CorpSimReplceReqModel
    {
        public CorpSimReplceData data { get; set; } = new CorpSimReplceData();
    }

    public class CorpSimReplceData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public CorpSimReplceAttributes attributes { get; set; } = new CorpSimReplceAttributes();
    }

    public class CorpSimReplceAttributes
    {
        [JsonProperty(PropertyName = "biometric-request-id")]
        public string biometric_request_id { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "new-icc")]
        public string new_icc { get; set; } = string.Empty;
        public string reason { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "payment-mode")]
        public string payment_mode { get; set; } = string.Empty;
        public CorpSimReplceMeta meta { get; set; } = new CorpSimReplceMeta();
    }

    public class CorpSimReplceMeta
    {
        public string channel { get; set; } = string.Empty;
        public string reseller { get; set; } = string.Empty;
        public string salesman { get; set; } = string.Empty;
    }
}
