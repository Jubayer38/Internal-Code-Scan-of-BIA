using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class CDTRequestModel
    {
        public CDTData data { get; set; } = new CDTData();
    }

    public class CDTData
    {
        public string type { get; set; } = string.Empty;
        public CDTAttributes attributes { get; set; } = new CDTAttributes();
    }

    public class CDTAttributes
    {
        [JsonProperty(PropertyName = "order-channel")]
        public string orderchannel { get; set; } = string.Empty;
        public CDTOrderer orderer { get; set; } = new CDTOrderer();
        public CDTOrder order { get; set; } = new CDTOrder();
    }

    public class CDTOrderer
    {
        [JsonProperty(PropertyName = "first-name")]
        public string firstname { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "last-name")]
        public string lastname { get; set; } = string.Empty;
        public string nationality { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "employment-type")]
        public string employmenttype { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "date-of-birth")]
        public string dateofbirth { get; set; } = string.Empty;
        //public string email { get; set; }
        [JsonProperty(PropertyName = "id-document-type")]
        public string iddocumenttype { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "id-document-number")]
        public string iddocumentnumber { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "home-phone-number")]
        public string homephonenumber { get; set; } = string.Empty;
    }

    public class CDTOrder
    {
        public string id { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "created-at")]
        public string createdat { get; set; } = string.Empty;
    }
}
