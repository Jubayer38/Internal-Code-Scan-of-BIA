using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class FTRRestrictionResponseModel
    {
        public FTRRespData[] data { get; set; } = Array.Empty<FTRRespData>();
    }
    public class FTRRespData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public FTRRespAttributes attributes { get; set; } = new FTRRespAttributes();
    }

    public class FTRRespAttributes
    {
        [JsonProperty("request-id")]
        public string request_id { get; set; } = string.Empty;
        public string href { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;  
        [JsonProperty("scheduled-at")]
        public DateTime scheduled_at { get; set; }
    }
}
