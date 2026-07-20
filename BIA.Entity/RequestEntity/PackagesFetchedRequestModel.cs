using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class PackagesFetchedRequestModel
    {
        public string offer_name { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public string subscription_id { get; set; } = string.Empty;
        public string subscription_name { get; set; } = string.Empty;
        public string lan { get; set; } = string.Empty;
        public string category_name { get; set; } = string.Empty;
        public string channel_name { get; set; } = string.Empty;
        public int right_id { get; set; }
        public int is_bp { get; set; }
        public string initiator_channel { get; set; } = string.Empty;
        public string order_type { get; set; } = string.Empty;
        public string subscription_type { get; set; } = string.Empty;
        public string simkit_type { get; set; } = string.Empty;
        public string ext_package_name { get; set; } = string.Empty;
        public string order_number { get; set; } = string.Empty;
    }
}
