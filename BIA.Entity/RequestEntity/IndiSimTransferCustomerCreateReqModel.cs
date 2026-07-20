using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class IndiSimTransferCustomerCreateReqModel
    {
        public IndiSimTransferCustomerCreateData data { get; set; } = new IndiSimTransferCustomerCreateData();
    }

    public class IndiSimTransferCustomerCreateData
    {
        public string type { get; set; } = string.Empty;

        public IndiSimTransferCustomerCreateAttributes attributes { get; set; } = new IndiSimTransferCustomerCreateAttributes();
    }

    public class IndiSimTransferCustomerCreateAttributes
    {
        [JsonProperty(PropertyName = "id-document-type")]
        public string id_document_type { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "id-document-number")]
        public string id_document_number { get; set; } = string.Empty;

        public string birthday { get; set; } = string.Empty;

        public string nationality { get; set; } = string.Empty;
    }
}
