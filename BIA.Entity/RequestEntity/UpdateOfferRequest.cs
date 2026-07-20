using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class UpdateOfferRequest
    {
        public string OriginNodeType { get; set; } = string.Empty;
        public string OriginHostName { get; set; } = string.Empty;  
        public string OriginTransactionID { get; set; } = string.Empty;
        public DateTime OriginTimeStamp { get; set; }
        public int SubscriberNumberNAI { get; set; }
        public long NegotiatedCapabilities { get; set; }
        public string SubscriberNumber { get; set; } = string.Empty;    
        public int OfferID { get; set; }
        public int OfferType { get; set; }
        public DateTime expiryDateTime { get; set; }
    }
}
