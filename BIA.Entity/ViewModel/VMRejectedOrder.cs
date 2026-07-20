using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMRejectedOrder
    {
        public string quality_control_id { get; set; } = string.Empty;
        public string customer_id { get; set; } = string.Empty;
        public string mobile_number { get; set; } = string.Empty;
        public string customer_name { get; set; } = string.Empty;

        public int division_id { get; set; }
        public string division_name { get; set; } = string.Empty;

        public int district_id { get; set; }
        public string district_name { get; set; } = string.Empty;

        public int thana_id { get; set; }
        public string thana_name { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;

        public string alt_msisdn { get; set; } = string.Empty;
        public string reject_reason { get; set; } = string.Empty;
        public string rejection_date { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;

        public string road_number { get; set; } = string.Empty;
        public string house_number { get; set; } = string.Empty;
        public string flat_number { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty; 
        public int is_over_due { get; set; }
    }
}
