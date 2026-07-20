using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class SubscriptionTypeReponseById
    {
        public List<SubscriptionTypeByIdReponseData> data { get; set; }
        public SubscriptionTypeReponseById()
        {
            data = new List<SubscriptionTypeByIdReponseData>();
        }
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
    }
    public class SubscriptionTypeByIdReponseData
    {
        /// <summary>
        /// 
        /// </summary>
        /// 
        public string subscription_type_id { get; set; } = string.Empty;
        public string subscription_type_name { get; set; } = string.Empty;
    }

    public class SubscriptionTypeReponseByIdRev
    {
        public List<SubscriptionTypeByIdReponseDataRev> data { get; set; } = new List<SubscriptionTypeByIdReponseDataRev>();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }
    public class SubscriptionTypeByIdReponseDataRev
    {
        public string subscription_type_id { get; set; } = string.Empty; 
        public string subscription_type_name { get; set; } = string.Empty;  

    }
}
