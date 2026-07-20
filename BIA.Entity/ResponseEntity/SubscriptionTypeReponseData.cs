using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    /// <summary>
    /// This class is used for geting the response of subscription type ID.
    /// </summary>
    public class SubscriptionTypeReponse
    {
        public List<SubscriptionTypeReponseData> data { get; set; }
        public SubscriptionTypeReponse()
        {
            data = new List<SubscriptionTypeReponseData>();
        }
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class SubscriptionTypeReponseData
    {
        /// <summary>
        /// 
        /// </summary>
        /// 
        public string subscription_id { get; set; } = string.Empty;
        public string subscription_name { get; set; } = string.Empty;

    }

    public class UnpairedMSISDNData
    {
        public List<ReponseData> data { get; set; }
        public UnpairedMSISDNData()
        {
            data = new List<ReponseData>();
        }
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class ReponseData
    {
        public string msisdn { get; set; } = string.Empty;
    }

    public class UnpairedMSISDNDataRev
    {
        public List<ReponseDataRev> data { get; set; } = new List<ReponseDataRev>();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class PairedMSISDNDataRev
    {
        public ReponseDataRev data { get; set; } = new ReponseDataRev();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;
    }

    public class ReponseDataRev
    {
        public string msisdn { get; set; } = string.Empty;
    }

    /// <summary>
    /// This class is used for geting the response of subscription type ID.
    /// </summary>
    public class SubscriptionTypeReponseRev
    {
        public List<SubscriptionTypeReponseDataRev> data { get; set; } = new List<SubscriptionTypeReponseDataRev>();
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty; 
    } 

    public class SubscriptionTypeReponseDataRev
    {
        /// <summary>
        /// 
        /// </summary>
        /// 
        public string subscription_id { get; set; } = string.Empty;
        public string subscription_name { get; set; } = string.Empty;   

    }
}
