using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{

    public class CustomerAddressResponse
    {
    }


    public class CustomerAddressResponseRootobject
    {
        public List<CustomerAddressResponseDatum> data { get; set; } = new List<CustomerAddressResponseDatum>();
    }

    public class CustomerAddressResponseDatum
    {
        [JsonProperty(PropertyName = "type")]
        public string type { get; set; } = string.Empty;
        public CustomerAddressResponseAttributes attributes { get; set; } = new CustomerAddressResponseAttributes();
        public string id { get; set; } = string.Empty;
        public CustomerAddressResponseLinks links { get; set; } = new CustomerAddressResponseLinks();
    }

    public class CustomerAddressResponseAttributes
    {
        public string city { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "postal-code")]
        public string postalcode { get; set; } = string.Empty;
        public string co { get; set; } = string.Empty;
        public string addresstype { get; set; } = string.Empty;
        public string apartment { get; set; } = string.Empty;
        public string validated { get; set; } = string.Empty;       
        public string country { get; set; } = string.Empty;
        public string building { get; set; } = string.Empty;
        public string county { get; set; } = string.Empty;
        public string lastmodified { get; set; } = string.Empty;
        public string floor { get; set; } = string.Empty;
        public string province { get; set; } = string.Empty;
        public CustomerAddressResponseCountryName countryname { get; set; } = new CustomerAddressResponseCountryName();
        public string postalbox { get; set; } = string.Empty;
        public string street { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string room { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "postal-district")]
        public string postaldistrict { get; set; } = string.Empty;
    }

    public class CustomerAddressResponseCountryName
    {
        public string fr { get; set; } = string.Empty;
        public string en { get; set; } = string.Empty;
        public string de { get; set; } = string.Empty;
        public string it { get; set; } = string.Empty;
    }

    public class CustomerAddressResponseLinks
    {
        public string self { get; set; } = string.Empty;    
    }
}
