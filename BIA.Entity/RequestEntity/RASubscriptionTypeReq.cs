using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class RASubscriptionTypeReq
    {
        public string retailer_id { get; set; } = string.Empty;
        /// <summary>
        /// prepaid/postpaid
        /// </summary>
        public string subscription_type { get; set; } = "";
        /// <summary>
        /// en/bn
        /// </summary>
        public string? lan { get; set; }
        /// <summary>
        /// Session Token
        /// </summary>
        public string session_token { get; set; } = string.Empty;
        public string channel_name { get; set; } = "";
    }

    public class RASubscriptionTypeReqWithMapping
    {
        public string retailer_id { get; set; } = string.Empty;
        /// <summary>
        /// prepaid/postpaid
        /// </summary>
        public string subscription_type { get; set; } = "";
        /// <summary>
        /// en/bn
        /// </summary>
        public string? lan { get; set; }
        /// <summary>
        /// Session Token
        /// </summary>
        public string session_token { get; set; } = string.Empty;
        public string channel_name { get; set; } = "";
        public int is_bp { get; set; }
        public int right_id { get; set; }
    }

    public class RASubscriptionTypeReqWithMappingV2
    {
        public string retailer_id { get; set; } = string.Empty;
        /// <summary>
        /// prepaid/postpaid
        /// </summary>
        public string subscription_type { get; set; } = "";
        /// <summary>
        /// en/bn
        /// </summary>
        public string? lan { get; set; }
        /// <summary>
        /// Session Token
        /// </summary>
        public string channel_name { get; set; } = "";
        public int is_bp { get; set; }
        public int right_id { get; set; }
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string ext_subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
        public string order_number { get; set; } = string.Empty;
    }
}
