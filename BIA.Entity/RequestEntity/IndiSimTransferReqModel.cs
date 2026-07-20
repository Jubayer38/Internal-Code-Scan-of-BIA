using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class IndiSimTransferReqModel
    {
        public IndiSimTransferReqData data { get; set; } = new IndiSimTransferReqData();
    }

    public class IndiSimTransferReqData
    {
        public string type { get; set; } = string.Empty;

        public string id { get; set; } = string.Empty;

        public IndiSimTransferReqAttributes attributes { get; set; } = new IndiSimTransferReqAttributes();
    }

    public class IndiSimTransferReqAttributes
    {
        [JsonProperty(PropertyName = "owner-customer")]
        public IndiSimTransferReqOwnerCustomer owner_customer { get; set; } = new IndiSimTransferReqOwnerCustomer();

        [JsonProperty(PropertyName = "biometric-request-id")]
        public string biometric_request_id { get; set; } = string.Empty;

        public IndiSimTransferReqMeta _meta { get; set; } = new IndiSimTransferReqMeta();
    }

    public class IndiSimTransferReqOwnerCustomer
    {
        public string id { get; set; } = string.Empty;

        public string type { get; set; } = string.Empty;
    }

    public class IndiSimTransferReqMeta
    {
        public string channel { get; set; } = string.Empty;
    }
}
