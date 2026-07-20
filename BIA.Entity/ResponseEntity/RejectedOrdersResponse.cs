using BIA.Entity.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    /// <summary>
    /// Rejected order's response type.
    /// </summary>
    public class RejectedOrdersResponse
    {
        /// <summary>
        /// Rejected orders data.
        /// </summary>
        public List<VMRejectedOrder> data { get; set; } = new List<VMRejectedOrder>();
        /// <summary>
        /// Data contains if api request success or not!
        /// </summary>
        public bool result { get; set; } = new bool();
        /// <summary>
        /// Data contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;
    }

    public class RejectedOrdersResponseRev
    {
        /// <summary>
        /// Rejected orders data. 
        /// </summary>
        public List<VMRejectedOrder> data { get; set; } = new List<VMRejectedOrder>();
        /// <summary>
        /// Data contains if api request success or not!
        /// </summary>
        public bool isError { get; set; }
        /// <summary>
        /// Data contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;
    } 

    public class RejectedOrdersRootobject
    {
        public List<RejectedOrdersDatum> data { get; set; } = new List<RejectedOrdersDatum>();
    }

    public class RejectedOrdersDatum
    {
        public RejectedOrdersAttributes attributes { get; set; } = new RejectedOrdersAttributes();      
        public RejectedOrdersRelationships relationships { get; set; } = new RejectedOrdersRelationships();
        public RejectedOrdersLinks3 links { get; set; } = new RejectedOrdersLinks3();
        public string id { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    public class RejectedOrdersAttributes
    {
        public string qcresponsible { get; set; } = string.Empty;
        public string channel { get; set; } = string.Empty;
        public string qcuser { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string reason { get; set; } = string.Empty;
        [JsonProperty(PropertyName = "last-modified")]
        public DateTime lastmodified { get; set; }
        [JsonProperty(PropertyName = "activation-time")]
        public DateTime activationtime { get; set; }
        public string status { get; set; } = string.Empty;
        public string reseller { get; set; } = string.Empty;
        public string confirmationcode { get; set; } = string.Empty;
    }

    public class RejectedOrdersRelationships
    {
        [JsonProperty(PropertyName = "owner-customer")]
        public RejectedOrdersOwnerCustomer ownercustomer { get; set; } = new RejectedOrdersOwnerCustomer();
        public RejectedOrdersSubscription subscription { get; set; } = new RejectedOrdersSubscription();
        [JsonProperty(PropertyName = "user-customer")]
        public RejectedOrdersUserCustomer usercustomer { get; set; } = new RejectedOrdersUserCustomer();
    }

    public class RejectedOrdersOwnerCustomer
    {
        public RejectedOrdersData data { get; set; } = new RejectedOrdersData();
        public RejectedOrdersLinks links { get; set; } = new RejectedOrdersLinks(); 
    }

    public class RejectedOrdersData
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class RejectedOrdersLinks
    {
        public string related { get; set; } = string.Empty;
    }

    public class RejectedOrdersSubscription
    {
        public RejectedOrdersData1 data { get; set; } = new RejectedOrdersData1();
        public RejectedOrdersLinks1 links { get; set; } = new RejectedOrdersLinks1();
    }

    public class RejectedOrdersData1
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class RejectedOrdersLinks1
    {
        public string related { get; set; } = string.Empty;
    }

    public class RejectedOrdersUserCustomer
    {
        public RejectedOrdersData2 data { get; set; } = new RejectedOrdersData2();
        public RejectedOrdersLinks2 links { get; set; } = new RejectedOrdersLinks2();
    }

    public class RejectedOrdersData2
    {
        public string type { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
    }

    public class RejectedOrdersLinks2
    {
        public string related { get; set; } = string.Empty;
    }

    public class RejectedOrdersLinks3
    {
        public string self { get; set; } = string.Empty;    
    }



}
