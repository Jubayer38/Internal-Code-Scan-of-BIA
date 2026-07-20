using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ViewModel
{
    public class VMOrderInfo
    {
        public string alt_msisdn { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public int thana_id { get; set; }   
        public string thana_name { get; set; } = string.Empty;
        public string road_number { get; set; } = string.Empty;
        public string flat_number { get; set; } = string.Empty;
        public string district_name { get; set; } = string.Empty;
        public int district_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string division_name { get; set; } = string.Empty;
        public int division_id { get; set; }
        public string house_number { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string postal_code { get; set; } = string.Empty;
        public string subscription_code { get; set; } = string.Empty;
        public string subscription_type_id { get; set; } = string.Empty;
        public string package_code { get; set; } = string.Empty;
        public int package_id { get; set; }
        public string salesman_code { get; set; } = string.Empty;
        public int is_urgent { get; set; }
        public string port_in_date { get; set; } = string.Empty;
    }
}
