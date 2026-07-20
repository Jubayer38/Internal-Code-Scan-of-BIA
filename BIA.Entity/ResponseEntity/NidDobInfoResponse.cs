using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class NidDobInfoResponse
    {
        public string dest_nid { get; set; } = string.Empty;
        public string dest_dob { get; set; } = string.Empty;
        public string src_nid { get; set; } = string.Empty;
        public string src_dob { get; set; } = string.Empty;
        public string old_sim_type { get; set; } = string.Empty;
        public string old_sim_number { get; set; } = string.Empty;
        public bool result { get; set; }
        public string message { get; set; } = string.Empty;
        public long? dbss_subscription_id { get; set; } = 0;
        public int src_sim_category { get; set; } = 0;
        public string src_owner_customer_id { get; set; } = string.Empty;
        public string src_user_customer_id { get; set; } = string.Empty;
        public string src_payer_customer_id { get; set; } = string.Empty;
    }
}
