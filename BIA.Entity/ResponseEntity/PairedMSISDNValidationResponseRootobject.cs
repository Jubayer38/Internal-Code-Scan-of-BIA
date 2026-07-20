using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class PairedMSISDNValidationResponseRootobject
    {
        public PairedMSISDNValidationResponseData data { get; set; } = new PairedMSISDNValidationResponseData();
    }

    public class PairedMSISDNValidationResponseData
    {
        public string type { get; set; } = string.Empty;
        public PairedMSISDNValidationResponseAttributes attributes { get; set; } = new PairedMSISDNValidationResponseAttributes();
    }

    public class PairedMSISDNValidationResponseAttributes
    {
        public string icc { get; set; } = string.Empty;
        public object price { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;

        [JsonProperty("number-category")]
        public string numbercategory { get; set; } = string.Empty;
        [JsonProperty("subscription-type")]
        public string subscriptionType { get; set; } = string.Empty;
        public object currency { get; set; } = string.Empty;
        public string imsi { get; set; } = string.Empty;
        public string salesman_id { get; set; } = string.Empty;
    }

}
