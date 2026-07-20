using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.RequestEntity
{
    public class SIMProductMappingReqModel
    {
        public int channel_id { get; set; }
        public int right_id { get; set; }
        public int is_bp { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public string mobile_number { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public string product_code { get; set; } = string.Empty;
    }
    
    public class SIMProductMappingReqModelV2
    {
        public int channel_id { get; set; }
        public int right_id { get; set; }
        public int is_bp { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public string mobile_number { get; set; } = string.Empty;
        public string retailer_id { get; set; } = string.Empty;
        public string product_code { get; set; } = string.Empty;
        public string ext_channel_type { get; set; } = string.Empty;
        public string ext_action_type { get; set; } = string.Empty;
        public string ext_sim_type { get; set; } = string.Empty;
        public string ext_storage_type { get; set; } = string.Empty;
    }
}
