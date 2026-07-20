using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class CustomerInfoUpdaterRequestRootobject
    {
        public CustomerInfoUpdaterRequestData data { get; set; } = new CustomerInfoUpdaterRequestData();
    }

    public class CustomerInfoUpdaterRequestData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public CustomerInfoUpdaterRequestAttributes attributes { get; set; } = new CustomerInfoUpdaterRequestAttributes();
    }

    public class CustomerInfoUpdaterRequestAttributes
    {
        public string email { get; set; } = string.Empty;
        public string alt_contact_phone { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "first-name")]
        public string firstname { get; set; } = string.Empty;

        public string gender { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "legal-address")]
        public CustomerInfoUpdaterRequestLegalAddress legaladdress { get; set; } = new CustomerInfoUpdaterRequestLegalAddress();
    }

    public class CustomerInfoUpdaterRequestLegalAddress
    {
        public string type { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;        
        [JsonProperty(PropertyName = "flat-number")]
        public string flatnumber { get; set; } = string.Empty;
        public string thana { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string division { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "house-number")]
        public string housenumber { get; set; } = string.Empty;
        public string district { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "post-code")]
        public string postcode { get; set; } = string.Empty;
    }
}
