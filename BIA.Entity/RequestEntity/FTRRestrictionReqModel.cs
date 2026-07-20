using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class FTRRestrictionReqModel
    {
        public FTRData data { get; set; } = new FTRData();
    }
    public class FTRMeta
    {
        public Dictionary<string, object> services { get; set; } = new Dictionary<string, object>();
        public string channel { get; set; } = string.Empty;
    }

    public class FTRData
    {
        [JsonProperty("type")]
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public FTRMeta meta { get; set; } = new FTRMeta();
    }
}
